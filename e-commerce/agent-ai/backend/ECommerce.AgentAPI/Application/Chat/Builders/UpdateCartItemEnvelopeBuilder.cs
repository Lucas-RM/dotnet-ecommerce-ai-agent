using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Chat.Builders;

public sealed class UpdateCartItemEnvelopeBuilder : IToolEnvelopeBuilder
{
    public string ToolName => "update_cart_item";

    public ChatEnvelope Build(JsonElement? data)
    {
        var total = EnvelopeJson.GetDecimal(data, "totalPrice");
        var intro = $"Quantidade atualizada. Total do carrinho: {EnvelopeJson.FormatMoney(total)}.";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Posso ajudar com mais alguma coisa?",
            ToolName: ToolName,
            DataType: "Cart",
            Data: data);
    }
}
