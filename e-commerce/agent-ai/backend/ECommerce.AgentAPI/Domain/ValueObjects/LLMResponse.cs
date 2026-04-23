using ECommerce.AgentAPI.Domain.Entities;

namespace ECommerce.AgentAPI.Domain.ValueObjects;

public sealed class LLMResponse
{
    public string? Text { get; set; }

    public bool HasToolCall { get; set; }

    public ToolCall? ToolCall { get; set; }

    /// <summary> Valores comuns: <c>stop</c>, <c>tool_calls</c>, <c>length</c>. </summary>
    public string FinishReason { get; set; } = "stop";
}
