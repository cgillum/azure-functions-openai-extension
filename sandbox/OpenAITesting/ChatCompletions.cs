// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using OpenAI;
using OpenAI.Builders;
using OpenAI.Managers;
using OpenAI.ObjectModels;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;
using OpenAI.ObjectModels.SharedModels;

namespace OpenAITesting;

static class ChatCompletions
{
    public static async Task Run()
    {
        OpenAIService openAiService = new(new OpenAiOptions()
        {
            ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
        });

        FunctionDefinition createTodoFn = new FunctionDefinitionBuilder("AddTodo", "Create a new todo task")
            .AddParameter("task", PropertyDefinition.DefineString("The task to be created, e.g. take out the garbage"))
            .Validate()
            .Build();

        FunctionDefinition getTodosFn = new FunctionDefinitionBuilder("GetTodos", "Fetch the list of previously created todo tasks")
            .Validate()
            .Build();

        string model = Models.Gpt_4;
        List<ChatMessage> messages = new()
        {
            ChatMessage.FromSystem("Don't make assumptions about what values to plug into functions. Ask for clarification if a user request is ambiguous."),
            //ChatMessage.FromUser("What have I accomplished today?")
            ChatMessage.FromUser("Remind me to call my dad"),
            //ChatMessage.FromUser("What should I do today?"),
        };

        Console.WriteLine("USER: " + messages.Last().Content);

        while (true)
        {
            ChatCompletionCreateResponse completionResult = await openAiService.ChatCompletion.CreateCompletion(
                new ChatCompletionCreateRequest
                {
                    Messages = messages,
                    Model = model,
                    Functions = new List<FunctionDefinition> { createTodoFn, getTodosFn },
                });

            if (!completionResult.Successful)
            {
                if (completionResult.Error is null)
                {
                    throw new Exception("Unknown Error");
                }

                Console.WriteLine($"{completionResult.Error.Code}: {completionResult.Error.Message}");
                return;
            }

            ChatChoiceResponse choice = completionResult.Choices.First();
            Console.WriteLine($"Message:        {choice.Message.Content}");

            FunctionCall? fn = choice.Message.FunctionCall;
            if (fn is null)
            {
                break;
            }

            Console.WriteLine($"Function call:  {fn.Name}");
            foreach (KeyValuePair<string, object> entry in fn.ParseArguments())
            {
                Console.WriteLine($"  {entry.Key}: {entry.Value}");
            }

            string result;
            switch (fn.Name)
            {
                case "AddTodo":
                    Console.WriteLine("  Creating todo...");
                    //result = "The function call executed successfully and returned no results. Let the user know that you completed the action";//"done";
                    result = "The function call failed. Let the user know and ask if they'd like you to try again";
                    break;
                case "GetTodos":
                    Console.WriteLine("  Fetching todos...");
                    result = "[\"take out the garbage\", \"call my dad\"]";
                    break;
                default:
                    Console.WriteLine($"  Don't know how to invoke {fn.Name}");
                    return;
            }

            Console.WriteLine($"  Result: {result}");
            if (string.IsNullOrEmpty(result))
            {
                break;
            }

            // Add the result and re-prompt the model
            messages.Add(ChatMessage.FromFunction(result, fn.Name));
            continue;
        }
    }
}
