// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Functions.Worker.Extensions.OpenAI.Embeddings;

namespace Functions.Worker.Extensions.OpenAI.Search;

public record SearchableDocument(
    string Title,
    EmbeddingsContext Embeddings)
{
    public ConnectionInfo? ConnectionInfo { get; set; }
}

public record ConnectionInfo(string ConnectionName, string CollectionName);
