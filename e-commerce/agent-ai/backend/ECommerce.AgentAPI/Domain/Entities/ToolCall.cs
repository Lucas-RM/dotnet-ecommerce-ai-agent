namespace ECommerce.AgentAPI.Domain.Entities;

public sealed class ToolCall
{
    public string Name { get; set; } = string.Empty;

    public Dictionary<string, object> Arguments { get; set; } = new();

    public string SessionId { get; set; } = string.Empty;

    public string? CorrelationId { get; set; }
}
