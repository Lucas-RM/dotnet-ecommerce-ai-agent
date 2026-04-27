using System.Text.Json.Serialization;

namespace ECommerce.AgentAPI.Models;

/// <summary>Corpo de <c>POST /api/agent/chat/session/clear</c> — só o identificador de sessão a libertar no servidor.</summary>
public sealed class ClearSessionRequest
{
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }
}
