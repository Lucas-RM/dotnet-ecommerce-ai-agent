namespace ECommerce.AgentAPI.Domain.Entities;

public sealed class PendingApproval
{
    public string SessionId { get; set; } = string.Empty;

    public ToolCall ToolCall { get; set; } = new();

    public string ApprovalMessage { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
