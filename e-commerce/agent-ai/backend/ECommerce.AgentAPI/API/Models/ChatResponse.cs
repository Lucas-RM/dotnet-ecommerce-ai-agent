using System.Text.Json.Serialization;

namespace ECommerce.AgentAPI.Models;

/// <summary>Resposta do Agent ao cliente de chat (ex.: widget Angular).</summary>
public sealed class ChatResponse
{
    [JsonPropertyName("reply")]
    public string Reply { get; set; } = string.Empty;

    [JsonPropertyName("requiresApproval")]
    public bool RequiresApproval { get; set; }

    [JsonPropertyName("pendingToolName")]
    public string? PendingToolName { get; set; }

    /// <summary>Provedor LLM ativo (appsettings <c>LLM:Provider</c>), p.ex. <c>openai</c> ou <c>google</c>.</summary>
    [JsonPropertyName("llmProvider")]
    public string? LlmProvider { get; set; }
}
