// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Functions.Worker.Extensions.OpenAI.Embeddings;

/// <summary>
/// Input binding attribute for converting function trigger input into OpenAI embeddings.
/// </summary>
/// <remarks>
/// More information on OpenAI embeddings can be found at
/// https://platform.openai.com/docs/guides/embeddings/what-are-embeddings.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class EmbeddingsInputAttribute : InputBindingAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmbeddingsInputAttribute"/> class with the specified input.
    /// </summary>
    /// <param name="input">The input source containing the data to generate embeddings for.</param>
    /// <param name="inputType">The type of the input.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <c>null</c>.</exception>
    public EmbeddingsInputAttribute(string input, InputType inputType)
    {
        this.Input = input ?? throw new ArgumentNullException(nameof(input));
        this.InputType = inputType;
    }

    /// <summary>
    /// Gets or sets the ID of the model to use.
    /// </summary>
    public string Model { get; set; } = "text-embedding-ada-002";

    /// <summary>
    /// Gets or sets the maximum number of characters to chunk the input into.
    /// </summary>
    /// <remarks>
    /// <para>
    /// At the time of writing, the maximum input tokens allowed for second-generation input embedding models
    /// like <c>text-embedding-ada-002</c> is 8191. 1 token is ~4 chars in English, which translates to roughly 32K 
    /// characters of English input that can fit into a single chunk.
    /// </para>
    /// </remarks>
    public int MaxChunkLength { get; set; } = 8 * 1024; // REVIEW: Is 8K a good default?

    /// <summary>
    /// Gets the input to generate embeddings for.
    /// </summary>
    public string Input { get; }

    /// <summary>
    /// Gets the type of the input.
    /// </summary>
    public InputType InputType { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the binding should throw if there is an error calling the OpenAI
    /// endpoint.
    /// </summary>
    /// <remarks>
    /// The default value is <c>true</c>. Set this to <c>false</c> to handle errors manually in the function code.
    /// </remarks>
    public bool ThrowOnError { get; set; } = true;
}
