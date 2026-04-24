using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Chat.Builders;

public sealed class CheckoutEnvelopeBuilder : IToolEnvelopeBuilder
{
    public string ToolName => "checkout";

    public ChatEnvelope Build(JsonElement? data)
    {
        var total = EnvelopeJson.GetDecimal(data, "totalAmount");
        var intro = $"Pedido concluído! Total: {EnvelopeJson.FormatMoney(total)}.";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Obrigado pela compra! Posso buscar novos produtos?",
            ToolName: ToolName,
            DataType: "Order",
            Data: data);
    }
}
