using System.Text.Json.Serialization;

namespace ECommerce.AgentAPI.Models;

/// <summary>
/// Identifica a tool executada e o tipo lógico de dado (<c>dataType</c>) que o frontend deve usar
/// para escolher o card de apresentação. Adicionar tool nova = novo <c>DataType</c>.
/// </summary>
public sealed class ChatToolInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string? DataType { get; set; }
}
