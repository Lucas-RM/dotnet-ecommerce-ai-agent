using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Catalog;

/// <summary>
/// <c>search_products</c> — listagem paginada / busca de produtos da loja. Sem aprovação; envelope
/// escolhe o intro conforme total vs. itens mostrados (e trata o caso "sem resultados" com texto
/// e outro dedicados). Execução em <c>ProductPlugin.SearchProductsAsync</c>.
/// </summary>
public sealed class SearchProductsTool : ITool
{
    public string Name => "search_products";

    public ChatEnvelope BuildEnvelope(JsonElement? data)
    {
        var total = EnvelopeJson.GetInt(data, "totalCount") ?? 0;
        var shown = EnvelopeJson.ArrayLength(data, "items");

        if (total == 0 || shown == 0)
        {
            return new ChatEnvelope(
                IntroMessage: "Não encontrei produtos com esse filtro.",
                OutroMessage: "Quer tentar outro termo ou categoria?",
                ToolName: Name,
                DataType: "PagedProducts",
                Data: data);
        }

        var intro = total == shown
            ? $"Encontrei {total} produto(s):"
            : $"Mostrando {shown} de {total} produto(s):";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Quer ver detalhes de algum ou adicionar ao carrinho?",
            ToolName: Name,
            DataType: "PagedProducts",
            Data: data);
    }
}
