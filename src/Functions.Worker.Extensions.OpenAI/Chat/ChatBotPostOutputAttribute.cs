// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;

namespace Functions.Worker.Extensions.OpenAI.Chat;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class ChatBotPostOutputAttribute : OutputBindingAttribute
{
    public ChatBotPostOutputAttribute(string id)
    {
        this.Id = id;
    }

    /// <summary>
    /// Gets the ID of the chat bot to update.
    /// </summary>
    public string Id { get; }
}

public record ChatBotPostRequest(string UserMessage)
{
    public string Id { get; set; } = string.Empty;
}