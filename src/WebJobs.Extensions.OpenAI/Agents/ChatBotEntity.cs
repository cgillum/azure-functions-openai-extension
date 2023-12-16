// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OpenAI.Interfaces;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;

namespace WebJobs.Extensions.OpenAI.Agents;

public interface IChatBotEntity
{
    void Initialize(ChatBotCreateRequest request);
    Task PostAsync(ChatBotPostRequest request);
}

// IMPORTANT: Do not change the names or order of these enum values!
[JsonConverter(typeof(StringEnumConverter))]
public enum ChatBotStatus
{
    Uninitialized,
    Active,
    Expired,
}

record struct MessageRecord(DateTime Timestamp, ChatMessage Message);

[JsonObject(MemberSerialization.OptIn)]
class ChatBotRuntimeState
{
    [JsonProperty("messages")]
    public List<MessageRecord>? ChatMessages { get; set; }

    [JsonProperty("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonProperty("status")]
    public ChatBotStatus Status { get; set; } = ChatBotStatus.Uninitialized;
}

[JsonObject(MemberSerialization.OptIn)]
class ChatBotEntity : IChatBotEntity
{
    readonly ILogger logger;
    readonly IOpenAIServiceProvider openAIServiceProvider;
    readonly IAssistantSkillInvoker skillInvoker;

    public ChatBotEntity(
        ILoggerFactory loggerFactory,
        IOpenAIServiceProvider openAIServiceProvider,
        IAssistantSkillInvoker skillInvoker)
    {
        // When initialized via dependency injection
        this.logger = loggerFactory.CreateLogger<ChatBotEntity>();
        this.openAIServiceProvider = openAIServiceProvider ?? throw new ArgumentNullException(nameof(openAIServiceProvider));
        this.skillInvoker = skillInvoker ?? throw new ArgumentNullException(nameof(skillInvoker));
    }

    [JsonConstructor]
    ChatBotEntity()
    {
        // For deserialization
        this.logger = null!;
        this.openAIServiceProvider = null!;
        this.skillInvoker = null!;
    }

    [JsonProperty("state")]
    public ChatBotRuntimeState? State { get; set; }

    public void Initialize(ChatBotCreateRequest request)
    {
        this.logger.LogInformation(
            "[{Id}] Creating new chat session with expiration = {Timestamp} and instructions = \"{Text}\"",
            Entity.Current.EntityId,
            request.ExpiresAt?.ToString("o") ?? "never",
            request.Instructions ?? "(none)");
        this.State = new ChatBotRuntimeState
        {
            ChatMessages = string.IsNullOrEmpty(request.Instructions) ?
                new List<MessageRecord>() :
                new List<MessageRecord>() { new(DateTime.UtcNow, ChatMessage.FromSystem(request.Instructions)) },
            ExpiresAt = request.ExpiresAt ?? DateTime.UtcNow.AddHours(24),
            Status = ChatBotStatus.Active,
        };
    }

    public async Task PostAsync(ChatBotPostRequest request)
    {
        if (this.State is null || this.State.Status != ChatBotStatus.Active)
        {
            this.logger.LogWarning("[{Id}] Ignoring message sent to an uninitialized or expired chat bot.", Entity.Current.EntityId);
            return;
        }

        if (request == null || string.IsNullOrWhiteSpace(request.UserMessage))
        {
            this.logger.LogWarning("[{Id}] Ignoring empty message.", Entity.Current.EntityId);
            return;
        }

        this.logger.LogInformation("[{Id}] Received message: {Text}", Entity.Current.EntityId, request.UserMessage);

        this.State.ChatMessages ??= new List<MessageRecord>();
        this.State.ChatMessages.Add(new(DateTime.UtcNow, ChatMessage.FromUser(request.UserMessage)));

        string modelOrDeployment = request.Model ?? Models.Gpt_3_5_Turbo;
        IOpenAIService service = this.openAIServiceProvider.GetService(modelOrDeployment);

        // We loop if the model returns function calls. Otherwise, we break right away.
        // TODO: Add protection against infinite loops, which could happen if the model
        //       always returns function calls. We could add a limit on the number of
        //       loops, or a timeout, or both. We could also add a limit on the number
        //       of tokens returned by the model, which would prevent a runnaway billing
        //       situation.
        while (true)
        {
            // Get the next response from the LLM
            ChatCompletionCreateRequest chatRequest = new()
            {
                Messages = this.State.ChatMessages.Select(item => item.Message).ToList(),
                Model = modelOrDeployment,
                Functions = this.skillInvoker.GetFunctionsDefinitions(),
            };

            ChatCompletionCreateResponse response = await service.ChatCompletion.CreateCompletion(chatRequest);
            if (!response.Successful)
            {
                // Throwing an exception will cause the entity to abort the current operation.
                // Any changes to the entity state will be discarded.
                Error error = response.Error ?? new Error() { Code = "Unspecified", MessageObject = "Unspecified error" };
                throw new ApplicationException($"The OpenAI {chatRequest.Model} engine returned a '{error.Code}' error: {error.Message}");
            }

            // We don't normally expect more than one message, but just in case we get multiple messages,
            // return all of them separated by two newlines.
            string replyMessage = string.Join(
                Environment.NewLine + Environment.NewLine,
                response.Choices.Select(choice => choice.Message.Content));

            this.logger.LogInformation(
                "[{Id}] Got LLM response consisting of {Count} tokens: {Text}",
                Entity.Current.EntityId,
                response.Usage.CompletionTokens,
                replyMessage);

            if (!string.IsNullOrWhiteSpace(replyMessage))
            {
                this.State.ChatMessages.Add(new(DateTime.UtcNow, ChatMessage.FromAssistant(replyMessage.Trim())));

                this.logger.LogInformation(
                    "[{Id}] Chat length is now {Count} messages",
                    Entity.Current.EntityId,
                    this.State.ChatMessages.Count);
            }

            // Check for function calls
            List<FunctionCall> functionCalls = response.Choices
                .Select(c => c.Message.FunctionCall!)
                .Where(f => f is not null)
                .ToList();
            if (functionCalls.Count == 0)
            {
                // No function calls, so we're done
                break;
            }

            // Loop case: found some functions to execute
            this.logger.LogInformation(
                "[{Id}] Found {Count} function call(s) in response",
                Entity.Current.EntityId,
                functionCalls.Count);

            // Invoke the function calls and add the responses to the chat history.
            List<Task<object>> tasks = new(capacity: functionCalls.Count);
            foreach (FunctionCall call in functionCalls)
            {
                // CONSIDER: Call these in parallel
                this.logger.LogInformation(
                    "[{Id}] Calling function '{Name}' with arguments: {Args}",
                    Entity.Current.EntityId,
                    call.Name,
                    call.Arguments);

                string? result;
                try
                {
                    // NOTE: In Consumption plans, calling a function from another function results in double-billing.
                    // CONSIDER: Use a background thread to invoke the action to avoid double-billing.
                    result = await this.skillInvoker.InvokeAsync(call);

                    this.logger.LogInformation(
                        "[{id}] Function '{Name}' returned the following content: {Content}",
                        Entity.Current.EntityId,
                        call.Name,
                        result);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(
                        ex,
                        "[{id}] Function '{Name}' failed with an unhandled exception",
                        Entity.Current.EntityId);

                    // CONSIDER: Automatic retries?
                    result = "The function call failed. Let the user know and ask if they'd like you to try again";
                }

                if (string.IsNullOrWhiteSpace(result))
                {
                    // When experimenting with gpt-4-0613, an empty result would cause the model to go into a
                    // function calling loop. By instead providing a result with some instructions, I was able
                    // to get the model to response to the user in a natural way.
                    result = "The function call succeeded. Let the user know that you completed the action.";
                }

                ChatMessage funcMessage = ChatMessage.FromFunction(result, call.Name);
                this.State.ChatMessages.Add(new(DateTime.UtcNow, funcMessage));
            }
        }
    }
}