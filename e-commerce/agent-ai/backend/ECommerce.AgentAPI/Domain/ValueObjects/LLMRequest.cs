using ECommerce.AgentAPI.Domain.Entities;

namespace ECommerce.AgentAPI.Domain.ValueObjects;

public sealed class LLMRequest
{
    /// <summary> Sessão do chat (o mesmo id que <c>GetOrCreate</c> / memória). </summary>
    public string SessionId { get; set; } = string.Empty;

    public string Input { get; set; } = string.Empty;

    public List<ChatMessage> History { get; set; } = new();

    public List<ToolDefinition> Tools { get; set; } = new();

    public string SystemPrompt { get; set; } = string.Empty;

    public float Temperature { get; set; } = 0.3f;

    public int MaxTokens { get; set; } = 1024;
}
