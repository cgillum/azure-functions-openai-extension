// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Kusto;
using Microsoft.Extensions.Logging;
using WebJobs.Extensions.OpenAI;

namespace CSharpInProcSamples.Demos;

public static class EmailPrompt
{
    public record EmbeddingsRequest(string FilePath);

    public record EmailEmbedding(
        string DocumentId,
        string Subject,
        string Text,
        IReadOnlyList<double> Embeddings,
        DateTime Timestamp);

    [FunctionName("EmailPrompt")]
    public static IActionResult IngestEmail(
        [HttpTrigger(AuthorizationLevel.Function, "post")] EmbeddingsRequest req,
        [Embeddings("{FilePath}", InputType.FilePath, MaxChunkLength = 512)] EmbeddingsContext embeddings,
        [Kusto("testing", TableName = "Emails", Connection = "KustoConnectionString")] out EmailEmbedding[] output,
        ILogger log)
    {
        string documentId = Guid.NewGuid().ToString("N");
        DateTime timestamp = DateTime.UtcNow;

        // Convert the embeddings into a record to write to Kusto
        output = new EmailEmbedding[embeddings.Response.Data.Count];
        for (int i = 0; i < embeddings.Response.Data.Count; i++)
        {
            output[i] = new EmailEmbedding(
                DocumentId: documentId,
                Subject: Path.GetFileNameWithoutExtension(req.FilePath),
                Text: embeddings.Request.Input ?? embeddings.Request.InputAsList![i],
                Embeddings: embeddings.Response.Data[i].Embedding,
                Timestamp: timestamp);
        }

        log.LogInformation("Writing {count} embedding(s) to 'Emails' Kusto table", output.Length);
        return new OkObjectResult(new
        {
            status = "success",
            count = output.Length,
        });
    }

    [FunctionName(nameof(PromptEmail))]
    public static IActionResult PromptEmail(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req)
    {
        // TODO: Create a "semantic search/prompt" binding (what should we call it?)
        //       that allows us to do a "one-shot" prompt using the Kusto data against
        //       the GPT model with a single input binding.
        throw new NotImplementedException();
    }
}