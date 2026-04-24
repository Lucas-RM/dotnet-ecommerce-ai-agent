using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Chat.Builders;

public sealed class SearchProductsEnvelopeBuilder : IToolEnvelopeBuilder
{
    public string ToolName => "search_products";

    public ChatEnvelope Build(JsonElement? data)
    {
        var total = EnvelopeJson.GetInt(data, "totalCount") ?? 0;
        var shown = EnvelopeJson.ArrayLength(data, "items");

        if (total == 0 || shown == 0)
        {
            return new ChatEnvelope(
                IntroMessage: "Não encontrei produtos com esse filtro.",
                OutroMessage: "Quer tentar outro termo ou categoria?",
                ToolName: ToolName,
                DataType: "PagedProducts",
                Data: data);
        }

        var intro = total == shown
            ? $"Encontrei {total} produto(s):"
            : $"Mostrando {shown} de {total} produto(s):";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Quer ver detalhes de algum ou adicionar ao carrinho?",
            ToolName: ToolName,
            DataType: "PagedProducts",
            Data: data);
    }
}
