using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Catalog;

/// <summary>
/// <c>add_cart_item</c> — adiciona produto ao carrinho. Exige aprovação: a mensagem enriquece
/// com <c>quantity</c>, <c>productName</c> e, quando o LLM sintetiza <c>unitPrice</c>, o valor
/// unitário. Envelope mostra o total atualizado após a confirmação.
/// Execução em <c>CartPlugin.AddCartItemAsync</c>.
/// </summary>
public sealed class AddCartItemTool : ITool
{
    public string Name => "add_cart_item";

    public bool RequiresApproval => true;

    public string BuildApprovalMessage(IReadOnlyDictionary<string, object?> arguments)
    {
        var qty = Math.Max(1, ApprovalFormatting.ArgInt(arguments, "quantity", 1));
        var label = ApprovalFormatting.ResolveProductLabel(arguments);

        if (ApprovalFormatting.TryGetUnitPriceDisplay(arguments, out var price))
        {
            return string.Format(
                ApprovalFormatting.Culture,
                "Deseja adicionar **{0}** unidade(s) de **{1}** (R$ {2}) ao seu carrinho?",
                qty,
                label,
                price);
        }

        return string.Format(
            ApprovalFormatting.Culture,
            "Deseja adicionar **{0}** unidade(s) de **{1}** ao seu carrinho?",
            qty,
            label);
    }

    public ChatEnvelope BuildEnvelope(JsonElement? data)
    {
        var total = EnvelopeJson.GetDecimal(data, "totalPrice");
        var intro = $"Adicionado ao carrinho! Total atualizado: {EnvelopeJson.FormatMoney(total)}.";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Quer continuar comprando ou finalizar o pedido?",
            ToolName: Name,
            DataType: "Cart",
            Data: data);
    }
}
