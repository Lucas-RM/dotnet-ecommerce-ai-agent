using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Chat.Builders;

public sealed class GetOrderEnvelopeBuilder : IToolEnvelopeBuilder
{
    public string ToolName => "get_order";

    public ChatEnvelope Build(JsonElement? data)
    {
        var status = EnvelopeJson.GetString(data, "status");
        var intro = string.IsNullOrWhiteSpace(status)
            ? "Detalhes do pedido:"
            : $"Detalhes do pedido (status: **{status}**):";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Posso ajudar com mais alguma coisa?",
            ToolName: ToolName,
            DataType: "Order",
            Data: data);
    }
}
