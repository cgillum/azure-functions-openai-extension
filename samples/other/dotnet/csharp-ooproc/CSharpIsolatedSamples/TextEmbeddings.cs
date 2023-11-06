// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Functions.Worker.Extensions.OpenAI;
using Functions.Worker.Extensions.OpenAI.Embeddings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CSharpIsolatedSamples;

/// <summary>
/// Examples of working with OpenAI embeddings.
/// </summary>
public class TextEmbeddings
{
    readonly ILogger<TextEmbeddings> logger;

    public TextEmbeddings(ILogger<TextEmbeddings> logger)
    {
        this.logger = logger;
    }

    public record EmbeddingsRequest(string RawText, string FilePath);

    /// <summary>
    /// Example showing how to use the <see cref="EmbeddingsInputAttribute"/> input binding to generate embeddings 
    /// for a raw text string.
    /// </summary>
    [Function(nameof(GenerateEmbeddings_Http_Request))]
    public IActionResult GenerateEmbeddings_Http_Request(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "embeddings")] HttpRequest req,
        [Microsoft.Azure.Functions.Worker.Http.FromBody] EmbeddingsRequest input,
        [EmbeddingsInput("{RawText}", InputType.RawText)] EmbeddingsContext embeddings)
    {
        this.logger.LogInformation(
            "Received {count} embedding(s) for input text containing {length} characters.",
            embeddings.Count,
            input.RawText.Length);

        // TODO: Store the embeddings into a database or other storage.

        return new OkObjectResult($"Generated {embeddings.Count} chunk(s) from source text");
    }

    /// <summary>
    /// Example showing how to use the <see cref="EmbeddingsInputAttribute"/> input binding to generate embeddings
    /// for text contained in a file on the file system.
    /// </summary>
    [Function(nameof(GetEmbeddings_Http_FilePath))]
    public IActionResult GetEmbeddings_Http_FilePath(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "embeddings-from-file")] HttpRequest req,
        [Microsoft.Azure.Functions.Worker.Http.FromBody] EmbeddingsRequest input,
        [EmbeddingsInput("{FilePath}", InputType.FilePath, MaxChunkLength = 512)] EmbeddingsContext embeddings)
    {
        this.logger.LogInformation(
            "Received {count} embedding(s) for input file '{path}'.",
            embeddings.Response.Data.Count,
            input.FilePath);

        // TODO: Store the embeddings into a database or other storage.

        return new OkObjectResult($"Generated {embeddings.Count} chunk(s) from source file");
    }
}
