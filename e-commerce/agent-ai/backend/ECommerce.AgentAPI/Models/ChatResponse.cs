namespace ECommerce.AgentAPI.Models;

/// <summary>Resposta do Agent ao widget de chat.</summary>
public sealed class ChatResponse
{
    public string Reply { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public string? PendingToolName { get; set; }
}
