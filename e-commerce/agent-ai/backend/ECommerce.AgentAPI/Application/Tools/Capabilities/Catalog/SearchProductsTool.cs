using System.Text.Json;
using ECommerce.AgentAPI.Application.Tools.Payloads.V1;
using ECommerce.AgentAPI.Application.Tools.Serialization;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Capabilities.Catalog;

/// <summary>
/// <c>search_products</c> — domínio <b>catálogo</b>. Listagem / busca de produtos. Execução em
/// <c>ProductPlugin.SearchProductsAsync</c>.
/// </summary>
public sealed class SearchProductsTool : ITool
{
    public string Name => "search_products";
    public string DataType => "PagedProducts";

    public ChatEnvelope BuildEnvelope(JsonElement? data)
    {
        var p = ToolPayloadJson.Deserialize<PagedListDataV1>(data);
        var total = p?.TotalCount ?? 0;
        var shown = p?.Items?.Count ?? ToolPayloadJson.ArrayLength(data, "items");

        if (total == 0 || shown == 0)
        {
            return new ChatEnvelope(
                IntroMessage: "Não encontrei produtos com esse filtro.",
                OutroMessage: "Quer tentar outro termo ou categoria?",
                ToolName: Name,
                DataType: DataType,
                Data: data);
        }

        var intro = total == shown
            ? $"Encontrei {total} produto(s):"
            : $"Mostrando {shown} de {total} produto(s):";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Quer ver detalhes de algum ou adicionar ao carrinho?",
            ToolName: Name,
            DataType: DataType,
            Data: data);
    }
}
