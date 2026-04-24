using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Chat.Builders;

public sealed class RemoveCartItemEnvelopeBuilder : IToolEnvelopeBuilder
{
    public string ToolName => "remove_cart_item";

    public ChatEnvelope Build(JsonElement? data)
    {
        var remaining = EnvelopeJson.ArrayLength(data, "items");
        if (remaining == 0)
        {
            return new ChatEnvelope(
                IntroMessage: "Item removido. Seu carrinho ficou vazio.",
                OutroMessage: "Quer que eu busque outros produtos?",
                ToolName: ToolName,
                DataType: "Cart",
                Data: data);
        }

        var total = EnvelopeJson.GetDecimal(data, "totalPrice");
        var intro = $"Item removido. Carrinho atualizado — {remaining} item(ns), total {EnvelopeJson.FormatMoney(total)}:";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Posso ajudar com mais alguma coisa?",
            ToolName: ToolName,
            DataType: "Cart",
            Data: data);
    }
}
