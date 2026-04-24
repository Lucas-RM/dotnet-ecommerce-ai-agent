using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Chat.Builders;

public sealed class GetCartEnvelopeBuilder : IToolEnvelopeBuilder
{
    public string ToolName => "get_cart";

    public ChatEnvelope Build(JsonElement? data)
    {
        var itemCount = EnvelopeJson.ArrayLength(data, "items");
        if (itemCount == 0)
        {
            return new ChatEnvelope(
                IntroMessage: "Seu carrinho está vazio.",
                OutroMessage: "Quer que eu busque produtos na loja?",
                ToolName: ToolName,
                DataType: "Cart",
                Data: data);
        }

        var total = EnvelopeJson.GetDecimal(data, "totalPrice");
        var intro = $"Seu carrinho — {itemCount} item(ns), total {EnvelopeJson.FormatMoney(total)}:";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Deseja finalizar a compra ou continuar comprando?",
            ToolName: ToolName,
            DataType: "Cart",
            Data: data);
    }
}
