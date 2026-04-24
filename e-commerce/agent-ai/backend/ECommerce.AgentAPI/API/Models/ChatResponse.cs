using System.Text.Json;
using System.Text.Json.Serialization;

namespace ECommerce.AgentAPI.Models;

/// <summary>
/// Resposta do Agent ao cliente de chat (ex.: widget Angular).
/// Contrato padronizado em três blocos: <c>introMessage</c> → <c>data</c> → <c>outroMessage</c>.
/// </summary>
public sealed class ChatResponse
{
    /// <summary>Texto introdutório exibido antes dos dados estruturados. Em respostas sem tool, contém a fala do LLM.</summary>
    [JsonPropertyName("introMessage")]
    public string? IntroMessage { get; set; }

    /// <summary>Texto final (follow-up) exibido depois dos dados estruturados. <c>null</c> quando não houver.</summary>
    [JsonPropertyName("outroMessage")]
    public string? OutroMessage { get; set; }

    /// <summary>Tool executada nesta resposta e seu <c>dataType</c> lógico. <c>null</c> quando for texto puro.</summary>
    [JsonPropertyName("tool")]
    public ChatToolInfo? Tool { get; set; }

    /// <summary>Dados estruturados (DTO cru) devolvidos pela tool. <c>null</c> quando não houver dados a apresentar.</summary>
    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }

    [JsonPropertyName("requiresApproval")]
    public bool RequiresApproval { get; set; }

    [JsonPropertyName("pendingToolName")]
    public string? PendingToolName { get; set; }

    /// <summary>Provedor LLM ativo (appsettings <c>LLM:Provider</c>), p.ex. <c>openai</c> ou <c>google</c>.</summary>
    [JsonPropertyName("llmProvider")]
    public string? LlmProvider { get; set; }
}
