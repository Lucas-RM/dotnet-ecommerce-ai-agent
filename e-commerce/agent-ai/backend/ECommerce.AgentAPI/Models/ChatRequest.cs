namespace ECommerce.AgentAPI.Models;

/// <summary>Corpo de POST /api/agent/chat.</summary>
public sealed class ChatRequest
{
    public Guid SessionId { get; set; }
    public string Message { get; set; } = string.Empty;
}
