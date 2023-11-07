// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Functions.Worker.Extensions.OpenAI.Chat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace CSharpIsolatedSamples;

// IMPORTANT: This sample unfortunately doesn't work. The chat bot will always enter a "Failed" state after creation.
//            Tracking issue: https://github.com/cgillum/azure-functions-openai-extension/issues/21

public class ChatBots
{
    public record CreateRequest(string Instructions);

    [Function(nameof(CreateChatBot))]
    public ChatBotCreateResult CreateChatBot(
        [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "chats/{chatId}")] HttpRequest httpReq,
        [FromBody] CreateRequest createReq,
        string chatId)
    {
        var responseJson = new { chatId };
        return new ChatBotCreateResult(
            new ChatBotCreateRequest(chatId, createReq.Instructions),
            new ObjectResult(responseJson) { StatusCode = 202 });
    }

    public class ChatBotCreateResult
    {
        public ChatBotCreateResult(ChatBotCreateRequest createChatBotRequest, IActionResult httpResponse)
        {
            this.CreateRequest = createChatBotRequest;
            this.HttpResponse = httpResponse;
        }

        [ChatBotCreateOutput]
        public ChatBotCreateRequest CreateRequest { get; set; }
        public IActionResult HttpResponse { get; set; }
    }

    [Function(nameof(GetChatState))]
    public ChatBotState GetChatState(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "chats/{chatId}")] HttpRequest req,
        string chatId,
        [ChatBotQueryInput("{chatId}", TimestampUtc = "{Query.timestampUTC}")] ChatBotState state)
    {
        return state;
    }

    [Function(nameof(PostUserResponse))]
    public async Task<ChatBotPostResult> PostUserResponse(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chats/{chatId}")] HttpRequest req,
        string chatId)
    {
        // Get the message from the raw request body
        using StreamReader reader = new(req.Body);
        string userMessage = await reader.ReadToEndAsync();

        if (string.IsNullOrEmpty(userMessage))
        {
            return new ChatBotPostResult(null, new BadRequestObjectResult(new { message = "Request body is empty" }));
        }

        return new ChatBotPostResult(
            new ChatBotPostRequest(userMessage),
            new AcceptedResult());
    }

    public class ChatBotPostResult
    {
        public ChatBotPostResult(ChatBotPostRequest? postRequest, IActionResult httpResponse)
        {
            this.PostRequest = postRequest;
            this.HttpResponse = httpResponse;
        }

        [ChatBotPostOutput("{chatId}")]
        public ChatBotPostRequest? PostRequest { get; set; }
        public IActionResult HttpResponse { get; set; }
    }
}
