// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;
using Microsoft.Azure.Functions.Worker.Extensions.Abstractions;
using OpenAI.ObjectModels.RequestModels;

namespace Functions.Worker.Extensions.OpenAI.Chat;

[AttributeUsage(AttributeTargets.Parameter)]
public class ChatBotQueryInputAttribute : InputBindingAttribute
{
    public ChatBotQueryInputAttribute(string id)
    {
        this.Id = id;
    }

    /// <summary>
    /// Gets the ID of the chat bot to query.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets or sets the timestamp of the earliest message in the chat history to fetch.
    /// The timestamp should be in ISO 8601 format - for example, 2023-08-01T00:00:00Z.
    /// </summary>
    public string TimestampUtc { get; set; } = string.Empty;
}

public record ChatBotState(
    string Id,
    bool Exists,
    ChatBotStatus Status,
    DateTime CreatedAt,
    DateTime LastUpdatedAt,
    int TotalMessages,
    IReadOnlyList<ChatMessage> RecentMessages);


// IMPORTANT: Do not change the names or order of these enum values!
public enum ChatBotStatus
{
    Uninitialized,
    Active,
    Expired,
}

record struct MessageRecord(DateTime Timestamp, ChatMessage Message);

class ChatBotRuntimeState
{
    [JsonPropertyName("messages")]
    public List<MessageRecord>? ChatMessages { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime ExpiresAt { get; set; }

    [JsonPropertyName("status")]
    public ChatBotStatus Status { get; set; } = ChatBotStatus.Uninitialized;
}