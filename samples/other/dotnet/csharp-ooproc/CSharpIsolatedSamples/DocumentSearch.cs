// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Functions.Worker.Extensions.OpenAI;
using Functions.Worker.Extensions.OpenAI.Embeddings;
using Functions.Worker.Extensions.OpenAI.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using FromBodyAttribute = Microsoft.Azure.Functions.Worker.Http.FromBodyAttribute;

namespace CSharpIsolatedSamples;

class DocumentSearch
{
    public record EmbeddingsRequest(string FilePath);
    public record SemanticSearchRequest(string Prompt);

    // REVIEW: There are several assumptions about how the Embeddings binding and the SemanticSearch bindings
    //         work together. We should consider creating a higher-level of abstraction for this.
    [Function(nameof(Ingest))]
    public IngestResult Ingest(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [FromBody] EmbeddingsRequest input,
        [EmbeddingsInput("{FilePath}", InputType.FilePath, Model = "text-embedding-ada-002-private")] EmbeddingsContext embeddings)
    {
        string title = Path.GetFileNameWithoutExtension(input.FilePath);
        return new IngestResult(
            new SearchableDocument(title, embeddings),
            new OkObjectResult(new { status = "success", title, chunks = embeddings.Count }));
    }

    public class IngestResult
    {
        public IngestResult(SearchableDocument? doc, IActionResult httpResponse)
        {
            this.Document = doc;
            this.HttpResponse = httpResponse;
        }

        [SemanticSearchOutput("KustoConnectionString", "Documents")]
        public SearchableDocument? Document { get; set; }
        public IActionResult HttpResponse { get; set; }
    }

    [Function(nameof(Prompt))]
    public static IActionResult Prompt(
        [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
        [FromBody] SemanticSearchRequest unused,
        [SemanticSearchInput("KustoConnectionString", "Documents", Query = "{Prompt}", EmbeddingsModel = "text-embedding-ada-002-private", ChatModel = "gpt-35-turbo")] SemanticSearchContext result)
    {
        return new ContentResult { Content = result.Response, ContentType = "text/plain" };
    }
}
