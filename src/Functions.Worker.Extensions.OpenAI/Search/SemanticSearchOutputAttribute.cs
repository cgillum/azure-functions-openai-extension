// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using OpenAI.ObjectModels;

namespace Functions.Worker.Extensions.OpenAI.Search;

/// <summary>
/// Binding attribute for semantic search (input bindings) and semantic document storage (output bindings).
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class SemanticSearchOutputAttribute : OutputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticSearchInputAttribute"/> class with the specified connection
    /// and collection names.
    /// </summary>
    /// <param name="connectionName">
    /// The name of an app setting or environment variable which contains a connection string value.
    /// </param>
    /// <param name="collection">The name of the collection or table to search or store.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either <paramref name="collection"/> or <paramref name="connectionName"/> are null.
    /// </exception>
    public SemanticSearchOutputAttribute(string connectionName, string collection)
    {
        this.ConnectionName = connectionName ?? throw new ArgumentNullException(nameof(connectionName));
        this.Collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    /// <summary>
    /// Gets or sets the name of an app setting or environment variable which contains a connection string value.
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    public string ConnectionName { get; set; }

    /// <summary>
    /// The name of the collection or table to search.
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    public string Collection { get; set; }

    /// <summary>
    /// Gets or sets the ID of the model to use for embeddings.
    /// The default value is "text-embedding-ada-002".
    /// </summary>
    /// <remarks>
    /// This property supports binding expressions.
    /// </remarks>
    public string EmbeddingsModel { get; set; } = Models.TextEmbeddingAdaV2;

    /// <summary>
    /// Gets or sets the number of knowledge items to inject into the <see cref="SystemPrompt"/>.
    /// </summary>
    public int MaxKnowledgeCount { get; set; } = 1;

    /// <summary>
    /// Gets or sets a value indicating whether the binding should throw if there is an error calling the OpenAI
    /// endpoint.
    /// </summary>
    /// <remarks>
    /// The default value is <c>true</c>. Set this to <c>false</c> to handle errors manually in the function code.
    /// </remarks>
    public bool ThrowOnError { get; set; } = true;
}
