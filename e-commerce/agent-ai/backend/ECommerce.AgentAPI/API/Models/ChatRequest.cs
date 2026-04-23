using System.Text.Json.Serialization;

namespace ECommerce.AgentAPI.Models;

/// <summary>Corpo de <c>POST /api/agent/chat</c> — <see cref="SessionId"/> e mensagem do utilizador.</summary>
public sealed class ChatRequest
{
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
