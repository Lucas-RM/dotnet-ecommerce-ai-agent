using System.Text.Json;
using System.Text.Json.Serialization;

namespace ECommerce.AgentAPI.Application.Tools.Payloads.V1;

/// <summary>
/// DTOs da porção <c>data</c> dos envelopes (versão 1) — espelham o contrato agregado da API v1
/// consumida pelos plugins. Evolução: incrementar <see cref="ToolPayloadV1.PayloadVersion"/> e
/// introduzir ficheiro <c>V2</c> com mapping explícito nas tools, sem reescrever histórico.
/// </summary>
public static class ToolPayloadV1
{
    public const int PayloadVersion = 1;
}

public sealed class CartDataV1
{
    [JsonPropertyName("items")]
    public List<JsonElement>? Items { get; set; }

    [JsonPropertyName("totalPrice")]
    public decimal? TotalPrice { get; set; }
}

public sealed class PagedListDataV1
{
    [JsonPropertyName("totalCount")]
    public int? TotalCount { get; set; }

    [JsonPropertyName("items")]
    public List<JsonElement>? Items { get; set; }
}

public sealed class ProductDataV1
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class OrderDataV1
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal? TotalAmount { get; set; }
}
