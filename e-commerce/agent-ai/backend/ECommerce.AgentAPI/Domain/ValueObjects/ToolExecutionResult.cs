namespace ECommerce.AgentAPI.Domain.ValueObjects;

public sealed class ToolExecutionResult
{
    public bool Success { get; set; }

    public string Output { get; set; } = string.Empty;

    public string? Error { get; set; }

    public static ToolExecutionResult NotFound(string toolName) =>
        new()
        {
            Success = false,
            Output = string.Empty,
            Error = $"Tool desconhecida: {toolName}"
        };
}
