// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Functions.Worker.Extensions.OpenAI.Embeddings;
using OpenAI.ObjectModels.ResponseModels;

namespace Functions.Worker.Extensions.OpenAI.Search;

/// <summary>
/// Input binding target for the <see cref="SemanticSearchAttribute"/>.
/// </summary>
/// <param name="Embeddings">The embeddings context associated with the semantic search.</param>
/// <param name="Chat">The chat response from the large language model.</param>
public record SemanticSearchContext(EmbeddingsContext Embeddings, ChatCompletionCreateResponse Chat)
{
    /// <summary>
    /// Gets the latest response message from the OpenAI Chat API.
    /// </summary>
    public string Response => this.Chat.Choices.Last().Message.Content;
}
