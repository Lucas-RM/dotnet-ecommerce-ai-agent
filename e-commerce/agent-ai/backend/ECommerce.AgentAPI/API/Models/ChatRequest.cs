using System.Text.Json.Serialization;
using System.Text.Json;

namespace ECommerce.AgentAPI.Models;

/// <summary>Corpo de <c>POST /api/agent/chat</c> — <see cref="SessionId"/> e mensagem do utilizador.</summary>
public sealed class ChatRequest
{
    [JsonPropertyName("agentId")]
    public string? AgentId { get; set; }

    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("approvalId")]
    public string? ApprovalId { get; set; }

    [JsonPropertyName("clientVersion")]
    public string? ClientVersion { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("channel")]
    public string? Channel { get; set; }

    [JsonPropertyName("metadata")]
    public JsonElement? Metadata { get; set; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }
}
