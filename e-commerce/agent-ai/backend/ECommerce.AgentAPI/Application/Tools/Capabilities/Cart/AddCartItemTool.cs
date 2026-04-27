using System.Text.Json;
using ECommerce.AgentAPI.Application.Tools.Payloads.V1;
using ECommerce.AgentAPI.Application.Tools.Serialization;
using ECommerce.AgentAPI.Application.Tools.Shared;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Capabilities.Cart;

/// <summary>
/// <c>add_cart_item</c> — domínio <b>carrinho</b>. Aprovação e envelope pós-adição. Execução em
/// <c>CartPlugin.AddCartItemAsync</c>.
/// </summary>
public sealed class AddCartItemTool : ITool
{
    public string Name => "add_cart_item";
    public string DataType => "Cart";

    public bool RequiresApproval => true;

    public string BuildApprovalMessage(IReadOnlyDictionary<string, object?> arguments)
    {
        var qty = Math.Max(1, ApprovalFormatting.ArgInt(arguments, "quantity", 1));
        var label = ApprovalFormatting.ResolveProductLabel(arguments);

        if (ApprovalFormatting.TryGetUnitPriceDisplay(arguments, out var price))
        {
            return string.Format(
                ApprovalFormatting.Culture,
                "Deseja adicionar **{0}** unidade(s) de **{1}** ({2}) ao seu carrinho?",
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
        var c = ToolPayloadJson.Deserialize<CartDataV1>(data);
        var total = c?.TotalPrice;
        var intro = $"Adicionado ao carrinho! Total atualizado: {ToolEnvelopeText.FormatMoney(total)}.";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Quer continuar comprando ou finalizar o pedido?",
            ToolName: Name,
            DataType: DataType,
            Data: data);
    }
}
