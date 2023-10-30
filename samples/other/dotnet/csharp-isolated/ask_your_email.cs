using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Extensions.AI;

namespace csharp_isolated
{
    public static class AskYourEmail
    {
        public record EmbeddingsRequest(string RawText, string FilePath);
        public record SemanticSearchRequest(string Prompt);

        // REVIEW: There are several assumptions about how the Embeddings binding and the SemanticSearch bindings
        //         work together. We should consider creating a higher-level of abstraction for this.
        [Function("IngestEmail")]
        public static async Task<IActionResult> IngestEmail(
            [HttpTrigger(AuthorizationLevel.Function, "post")] EmbeddingsRequest req,
            [Embeddings(("{FilePath}", InputType.FilePath, 
            Model = "%AZURE_OPENAI_EMBEDDINGS_DEPLOYMENT%"))] EmbeddingsContext embeddings,
            [SemanticSearch("KustoConnectionString", "Documents")] IAsyncCollector<SearchableDocument> output)
        {
            string title = Path.GetFileNameWithoutExtension(req.FilePath);
            await output.AddAsync(new SearchableDocument(title, embeddings));
            return new OkObjectResult(new { status = "success", title, chunks = embeddings.Count });
        }

        [Function("PromptEmail")]
        public static IActionResult PromptEmail(
            [HttpTrigger(AuthorizationLevel.Function, "post")] SemanticSearchRequest unused,
            [SemanticSearch("KustoConnectionString", "Documents", 
            Query = "{Prompt}", 
            EmbeddingsModel = "%AZURE_OPENAI_EMBEDDINGS_DEPLOYMENT%", 
            ChatModel = "%AZURE_OPENAI_CHATGPT_DEPLOYMENT%")] 
            SemanticSearchContext result)
        {
            return new ContentResult { Content = result.Response, ContentType = "text/plain" };
        }

    }
}
