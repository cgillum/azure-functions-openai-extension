﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using OpenAI.GPT3.ObjectModels.RequestModels;
using OpenAI.GPT3.ObjectModels.ResponseModels;

namespace WebJobs.Extensions.OpenAI;

/// <summary>
/// Binding target for the <see cref="EmbeddingsAttribute"/>.
/// </summary>
/// <param name="Request">The embeddings request that was sent to OpenAI.</param>
/// <param name="Response">The embeddings response that was received from OpenAI.</param>
public record EmbeddingsContext(EmbeddingCreateRequest Request, EmbeddingCreateResponse Response);
