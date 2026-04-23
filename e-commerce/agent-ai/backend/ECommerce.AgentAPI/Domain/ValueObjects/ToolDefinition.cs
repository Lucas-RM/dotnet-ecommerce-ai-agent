namespace ECommerce.AgentAPI.Domain.ValueObjects;

public sealed class ToolDefinition
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public List<ToolParameter> Parameters { get; set; } = new();
}
