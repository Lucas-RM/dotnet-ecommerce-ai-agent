namespace ECommerce.AgentAPI.Domain.ValueObjects;

/// <summary> Parâmetro de schema de função (nome, tipo, descrição) para <see cref="ToolDefinition"/>. </summary>
public sealed class ToolParameter
{
    public string Name { get; set; } = string.Empty;

    public string Type { get; set; } = "string";

    public string? Description { get; set; }

    public bool Required { get; set; }
}
