# Azure Functions bindings for OpenAI's GPT engine

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Build](https://github.com/cgillum/azure-functions-openai-extension/actions/workflows/build.yml/badge.svg)](https://github.com/cgillum/azure-functions-openai-extension/actions/workflows/build.yml)

This is an **experimental** project that adds support for [OpenAI](https://platform.openai.com/) GPT-3 bindings in [Azure Functions](https://azure.microsoft.com/products/functions/). It is not currently endorsed or supported by Microsoft.

This extension depends on the [Betalgo.OpenAI](https://github.com/betalgo/openai) by [Betalgo](https://github.com/betalgo).

## NuGet Packages

The following NuGet packages are available as part of this project.

[![NuGet](https://img.shields.io/nuget/v/CGillum.WebJobs.Extensions.OpenAI.svg?label=webjobs.extensions.openai)](https://www.nuget.org/packages/CGillum.WebJobs.Extensions.OpenAI)<br/>
[![NuGet](https://img.shields.io/nuget/v/CGillum.WebJobs.Extensions.OpenAI.DurableTask.svg?label=webjobs.extensions.openai.durabletask)](https://www.nuget.org/packages/CGillum.WebJobs.Extensions.OpenAI.DurableTask)<br/>
[![NuGet](https://img.shields.io/nuget/v/CGillum.WebJobs.Extensions.OpenAI.Kusto.svg?label=webjobs.extensions.openai.kusto)](https://www.nuget.org/packages/CGillum.WebJobs.Extensions.OpenAI.Kusto)

## Requirements

* [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) or greater (Visual Studio 2022 recommended)
* [Azure Functions Core Tools v4.x](https://learn.microsoft.com/azure/azure-functions/functions-run-local?tabs=v4%2Cwindows%2Cnode%2Cportal%2Cbash)
* An OpenAI account and an [API key](https://platform.openai.com/account/api-keys) saved into a `OPENAI_API_KEY` environment variable

## Features

The following features are currently available. More features will be slowly added over time.

* [Text completions](#text-completion-input-binding)
* [Chat bots](#chat-bots)
* [Embeddings generators](#embeddings-generator)
* [Semantic search](#semantic-search)

### Text completion input binding

The `textCompletion` input binding can be used to invoke the [OpenAI Text Completions API](https://platform.openai.com/docs/guides/completion) and return the results to the function.

The examples below define "who is" HTTP-triggered functions with a hardcoded `"who is {name}?"` prompt, where `{name}` is the substituted with the value in the HTTP request path. The OpenAI input binding invokes the OpenAI GPT endpoint to surface the answer to the prompt to the function, which then returns the result text as the response content.

#### [C# example](./samples/dotnet/csharp-inproc/)

```csharp
[FunctionName(nameof(WhoIs))]
public static string WhoIs(
    [HttpTrigger(AuthorizationLevel.Function, Route = "whois/{name}")] HttpRequest req,
    [TextCompletion("Who is {name}?")] CompletionCreateResponse response)
{
    return response.Choices[0].Text;
}
```

#### [TypeScript example](./samples/nodejs/)

```typescript
import { app, input } from "@azure/functions";

// This OpenAI completion input requires a {name} binding value.
const openAICompletionInput = input.generic({
    prompt: 'Who is {name}?',
    maxTokens: '100',
    type: 'textCompletion'
})

app.http('whois', {
    methods: ['GET'],
    route: 'whois/{name}',
    authLevel: 'function',
    extraInputs: [openAICompletionInput],
    handler: async (_request, context) => {
        var response: any = context.extraInputs.get(openAICompletionInput)
        return { body: response.choices[0].text.trim() }
    }
});
```

You can run the above function locally using the Azure Functions Core Tools and sending an HTTP request, similar to the following:

```http
GET http://localhost:7127/api/whois/pikachu
```

The result that comes back will include the response from the GPT language model:

```text
HTTP/1.1 200 OK
Content-Type: text/plain; charset=utf-8
Date: Tue, 28 Mar 2023 18:25:40 GMT
Server: Kestrel
Transfer-Encoding: chunked

Pikachu is a fictional creature from the Pok�mon franchise. It is a yellow
mouse-like creature with powerful electrical abilities and a mischievous
personality. Pikachu is one of the most iconic and recognizable characters
from the franchise, and is featured in numerous video games, anime series,
movies, and other media.
```

You can find more instructions for running the samples in the corresponding project directories. The goal is to have samples for all languages supported by Azure Functions.

### Chat bots

[Chat completions](https://platform.openai.com/docs/guides/chat) are useful for building AI-powered chat bots. Unlike text completions, however, chat completions are inherently stateful and don't fit with the typical input/output binding model. To support chat completions, this extension automatically adds a built-in `OpenAI::GetNextChatCompletion` activity function that can be used by [Durable Functions](https://learn.microsoft.com/azure/azure-functions/durable/durable-functions-overview) apps in any language to manage the long-running state of the chat session.

In the examples below, you'll see a `ChatBotOrchestration` function that:

1. Takes a "system prompt" as an input, which provides initial instructions to the bot.
1. Defines a simple loop for interacting with the chat bot via external event messages.
1. Saves the chat bot's responses as custom status payloads.
1. Runs for as long as 24-hours, but can be allowed to run for longer or shorter as necessary (durable orchestrations have no time limits).

The pattern is reusable across a variety of chat bot scenarios. The only thing that changes is the system prompt that is passed to the bot.

#### [C# chat bot example](./samples/dotnet/csharp-inproc/ChatBot.cs)

This example uses a `GetChatCompletionAsync` extension method of the `IDurableOrchestrationContext` interface to invoke the built-in OpenAI chat completion activity function in a type-safe manner.

```csharp
[FunctionName(nameof(ChatBotOrchestration))]
public static async Task ChatBotOrchestration(
    [OrchestrationTrigger] IDurableOrchestrationContext context)
{
    static async Task SessionLoop(IDurableOrchestrationContext context, string message, Task timeoutTask)
    {
        // Chat history is stored locally in memory and passed to the activity function for each iteration.
        // This is required because ChatGPT is largely stateless and otherwise won't remember previous replies.
        // The first message is a system message that instructs the bot about how it should behave.
        List<ChatMessage> chatHistory = new(capacity: 100) { ChatMessage.FromSystem(message) };

        while (!timeoutTask.IsCompleted)
        {
            // Get the next prompt from ChatGPT. We save it into custom status so that a client can query it
            // and display it to the end user in an appropriate format.
            string assistantMessage = await context.GetChatCompletionAsync(chatHistory);
            chatHistory.Add(ChatMessage.FromAssistant(assistantMessage));
            context.SetCustomStatus(assistantMessage);

            // Wait for the user to respond. This is done by listening for an external event of a well-known name.
            // The payload of the external event is a message to add to the chat history.
            message = await context.WaitForExternalEvent<string>(name: "UserResponse");
            chatHistory.Add(ChatMessage.FromUser(message));
        }
    }

    // Create a timer that expires after 24 hours, which will be used to terminate the session loop.
    using CancellationTokenSource cts = new();
    Task timeoutTask = context.CreateTimer(context.CurrentUtcDateTime.AddHours(24), cts.Token);

    // Start the session loop. The loop will end when the timeout expires or if some other input causes the
    // session loop to end on its own.
    string message = context.GetInput<string>();
    Task sessionTask = SessionLoop(context, message, timeoutTask);
    await Task.WhenAny(timeoutTask, sessionTask);
    cts.Cancel();
}
```

### Embeddings Generator

OpenAI's [text embeddings](https://platform.openai.com/docs/guides/embeddings) measure the relatedness of text strings. Embeddings are commonly used for:

* **Search** (where results are ranked by relevance to a query string)
* **Clustering** (where text strings are grouped by similarity)
* **Recommendations** (where items with related text strings are recommended)
* **Anomaly detection** (where outliers with little relatedness are identified)
* **Diversity measurement** (where similarity distributions are analyzed)
* **Classification** (where text strings are classified by their most similar label)

Processing of the source text files typically involves chunking the text into smaller pieces, such as sentences or paragraphs, and then making an OpenAI call to produce embeddings for each chunk independently. Finally, the embeddings need to be stored in a database or other data store for later use.

#### [C# embeddings generator example](./samples/dotnet/csharp-inproc/EmbeddingsGenerator.cs)

```csharp
[FunctionName(nameof(GenerateEmbeddings_Http_Request))]
public static void GenerateEmbeddings_Http_Request(
    [HttpTrigger(AuthorizationLevel.Function, "post", Route = "embeddings")] EmbeddingsRequest req,
    [Embeddings("{RawText}", InputType.RawText)] EmbeddingCreateResponse embeddingsResponse,
    ILogger logger)
{
    logger.LogInformation(
        "Received {count} embedding(s) for input text containing {length} characters.",
        embeddingsResponse.Data.Count,
        req.RawText.Length);

    // TODO: Store the embeddings into a database or other storage.
}
```

### Semantic Search

The semantic search feature allows you to import documents into a vector database using an output binding and query the documents in that database using an input binding. For example, you can have a function that imports documents into a vector database and another function that issues queries to OpenAI using content stored in the vector database as context (also known as the Retrieval Augmented Generation, or RAG technique).

 The supported list of vector databases is extensible, and more can be added by authoring a specially crafted NuGet package. Currently supported vector databases include:

 * [Azure Data Explorer](https://azure.microsoft.com/services/data-explorer/) - See [this project](./src/WebJobs.Extensions.OpenAI.Kusto/)

 More may be added over time.

#### [C# document storage example](./samples/dotnet/csharp-inproc/Demos/EmailPromptDemo.cs)

This HTTP trigger function takes a path to a local file as input, generates embeddings for the file, and stores the result into [Azure Data Explorer](https://azure.microsoft.com/services/data-explorer/) (a.k.a. Kusto).

```csharp
public record EmbeddingsRequest(string FilePath);

[FunctionName("IngestEmail")]
public static async Task<IActionResult> IngestEmail_Better(
    [HttpTrigger(AuthorizationLevel.Function, "post")] EmbeddingsRequest req,
    [Embeddings("{FilePath}", InputType.FilePath)] EmbeddingsContext embeddings,
    [SemanticSearch("KustoConnectionString", "Documents")] IAsyncCollector<SearchableDocument> output)
{
    string title = Path.GetFileNameWithoutExtension(req.FilePath);
    await output.AddAsync(new SearchableDocument(title, embeddings));
    return new OkObjectResult(new { status = "success", title, chunks = embeddings.Count });
}
```

#### [C# document query example](./samples/dotnet/csharp-inproc/Demos/EmailPromptDemo.cs)

This HTTP trigger function takes a query prompt as input, pulls in semantically similar document chunks into a prompt, and then sends the combined prompt to OpenAI. The results are then made available to the function, which simply returns that chat response to the caller.

```csharp
public record SemanticSearchRequest(string Prompt);

[FunctionName("PromptEmail")]
public static IActionResult PromptEmail(
    [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
    [SemanticSearch("KustoConnectionString", "Documents", Query = "{Prompt}")] SemanticSearchContext result)
{
    return new ContentResult { Content = result.Response, ContentType = "text/plain" };
}
```

The responses from the above function will be based on relevant document snippets which were previously uploaded to the vector database. For example, assuming you uploaded internal emails discussing a new feature of Azure Functions that supports OpenAI, you could issue a query similar to the following:

```http
POST http://localhost:7127/api/PromptEmail
Content-Type: application/json

{
    "Prompt": "Was a decision made to officially release an OpenAI binding for Azure Functions?"
}
```

And you might get a response that looks like the following (actual results may vary):

```text
HTTP/1.1 200 OK
Content-Length: 454
Content-Type: text/plain

There is no clear decision made on whether to officially release an OpenAI binding for Azure Functions as per the email "Thoughts on Functions+AI conversation" sent by Bilal. However, he suggests that the team should figure out if they are able to free developers from needing to know the details of AI/LLM APIs by sufficiently/elegantly designing bindings to let them do the "main work" they need to do. Reference: Thoughts on Functions+AI conversation.
```
