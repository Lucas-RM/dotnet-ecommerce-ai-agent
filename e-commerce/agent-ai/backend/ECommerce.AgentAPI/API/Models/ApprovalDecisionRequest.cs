using System.Text.Json.Serialization;

namespace ECommerce.AgentAPI.Models;

public sealed class ApprovalDecisionRequest
{
    [JsonPropertyName("sessionId")]
    public Guid SessionId { get; set; }

    [JsonPropertyName("decision")]
    public string Decision { get; set; } = string.Empty;

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; set; }
}
