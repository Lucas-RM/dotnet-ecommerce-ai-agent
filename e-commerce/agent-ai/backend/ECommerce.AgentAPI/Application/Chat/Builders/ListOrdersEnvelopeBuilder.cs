using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Chat.Builders;

public sealed class ListOrdersEnvelopeBuilder : IToolEnvelopeBuilder
{
    public string ToolName => "list_orders";

    public ChatEnvelope Build(JsonElement? data)
    {
        var total = EnvelopeJson.GetInt(data, "totalCount") ?? 0;
        if (total == 0)
        {
            return new ChatEnvelope(
                IntroMessage: "Você ainda não tem pedidos.",
                OutroMessage: "Posso ajudar a encontrar produtos?",
                ToolName: ToolName,
                DataType: "PagedOrders",
                Data: data);
        }

        var shown = EnvelopeJson.ArrayLength(data, "items");
        var intro = total == shown
            ? $"Seus pedidos ({total}):"
            : $"Mostrando {shown} de {total} pedido(s):";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Quer abrir algum pedido específico?",
            ToolName: ToolName,
            DataType: "PagedOrders",
            Data: data);
    }
}
