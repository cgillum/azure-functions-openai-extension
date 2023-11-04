using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.OpenAI;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace csharp_isolated
{
    public class embeddings
    {
        private readonly ILogger _logger;

        public embeddings(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<embeddings>();
        }

        [Function("embeddings")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            [EmbeddingsInput("{FilePath}", InputType.FilePath,
            Model = "%AZURE_OPENAI_EMBEDDINGS_DEPLOYMENT%")] EmbeddingsContext embeddings)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            response.WriteString(embeddings.Count.ToString());

            return response;
        }
    }
}
