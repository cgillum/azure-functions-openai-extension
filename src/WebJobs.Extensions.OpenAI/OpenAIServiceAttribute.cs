// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Azure.WebJobs.Description;
using OpenAI.Managers;

namespace Microsoft.Azure.Functions.Worker.Extensions.AI;

/// <summary>
/// Input binding attribute for getting an instance of the <see cref="OpenAIService"/> class.
/// </summary>
/// <remarks>
/// WARNING: This may be removed in a future version.
/// </remarks>
public sealed class OpenAIServiceAttribute : Attribute
{
}
