using ECommerce.AgentAPI.Domain.Entities;

namespace ECommerce.AgentAPI.Domain.ValueObjects;

public sealed class LLMResponse
{
    public string? Text { get; set; }

    public bool HasToolCall { get; set; }

    public ToolCall? ToolCall { get; set; }

    /// <summary>
    /// Invocações a plugins que correram durante <c>GetChatMessageContentsAsync</c> (auto-invocation),
    /// para o caso de uso montar envelope e preencher <c>tool</c> / <c>data</c> na resposta.
    /// </summary>
    public IReadOnlyList<RecordedToolInvocation> RecordedToolExecutions { get; set; } =
        Array.Empty<RecordedToolInvocation>();

    /// <summary> Valores comuns: <c>stop</c>, <c>tool_calls</c>, <c>length</c>. </summary>
    public string FinishReason { get; set; } = "stop";
}
