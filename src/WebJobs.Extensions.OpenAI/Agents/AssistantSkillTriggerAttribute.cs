﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Description;

namespace WebJobs.Extensions.OpenAI.Agents;

[Binding]
[AttributeUsage(AttributeTargets.Parameter)]
public class AssistantSkillTriggerAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssistantSkillTriggerAttribute"/> class with the specified function
    /// description.
    /// </summary>
    /// <param name="functionDescription">A description of the assistant function, which is provided to the model.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="functionDescription"/> is <c>null</c>.</exception>
    public AssistantSkillTriggerAttribute(string functionDescription)
    {
        this.FunctionDescription = functionDescription ?? throw new ArgumentNullException(nameof(functionDescription));
    }

    /// <summary>
    /// Gets or sets the name of the function to be invoked by the assistant.
    /// </summary>
    public string? FunctionName { get; set;  }

    /// <summary>
    /// Gets the description of the assistant function, which is provided to the LLM.
    /// </summary>
    public string FunctionDescription { get; }

    // TODO: Consider making the function description another trigger type to make it work across all languages

    /// <summary>
    /// Gets or sets a description of the function parameter, which is provided to the LLM.
    /// </summary>
    public string? ParameterDescription { get; set; }

    /// <summary>
    /// Gets or sets the OpenAI chat model to use.
    /// </summary>
    /// <remarks>
    /// When using Azure OpenAI, then should be the name of the model <em>deployment</em>.
    /// </remarks>
    public string Model { get; set; } = "gpt-3.5-turbo";
}
