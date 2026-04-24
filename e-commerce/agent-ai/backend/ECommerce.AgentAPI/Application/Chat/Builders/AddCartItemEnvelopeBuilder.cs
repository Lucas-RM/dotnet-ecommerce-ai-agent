using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Chat.Builders;

/// <summary>
/// Após a execução, a API devolve o <c>CartDto</c> completo — por isso o <c>DataType</c> é <c>Cart</c>.
/// </summary>
public sealed class AddCartItemEnvelopeBuilder : IToolEnvelopeBuilder
{
    public string ToolName => "add_cart_item";

    public ChatEnvelope Build(JsonElement? data)
    {
        var total = EnvelopeJson.GetDecimal(data, "totalPrice");
        var intro = $"Adicionado ao carrinho! Total atualizado: {EnvelopeJson.FormatMoney(total)}.";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Quer continuar comprando ou finalizar o pedido?",
            ToolName: ToolName,
            DataType: "Cart",
            Data: data);
    }
}
