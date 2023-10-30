// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
//using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using OpenAI.ObjectModels;

namespace Microsoft.Azure.Functions.Worker.Extensions.AI;

/// <summary>
/// Binding attribute for semantic search (input bindings) and semantic document storage (output bindings).
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class SemanticSearchOutputAttribute : OutputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticSearchAttribute"/> class with the specified connection
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

    public bool ThrowOnError { get; set; } = true;
}
