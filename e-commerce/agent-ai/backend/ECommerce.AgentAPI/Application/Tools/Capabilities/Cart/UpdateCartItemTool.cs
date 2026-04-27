using System.Text.Json;
using ECommerce.AgentAPI.Application.Tools.Payloads.V1;
using ECommerce.AgentAPI.Application.Tools.Serialization;
using ECommerce.AgentAPI.Application.Tools.Shared;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Capabilities.Cart;

public sealed class UpdateCartItemTool : ITool
{
    public string Name => "update_cart_item";
    public string DataType => "Cart";

    public bool RequiresApproval => true;

    public string BuildApprovalMessage(IReadOnlyDictionary<string, object?> arguments)
    {
        var qty = Math.Max(1, ApprovalFormatting.ArgInt(arguments, "quantity", 1));
        var label = ApprovalFormatting.ResolveProductLabel(arguments);

        return string.Format(
            ApprovalFormatting.Culture,
            "Deseja atualizar a quantidade de **{0}** para **{1}** unidade(s)?",
            label,
            qty);
    }

    public ChatEnvelope BuildEnvelope(JsonElement? data)
    {
        var c = ToolPayloadJson.Deserialize<CartDataV1>(data);
        var total = c?.TotalPrice;
        var intro = $"Quantidade atualizada. Total do carrinho: {ToolEnvelopeText.FormatMoney(total)}.";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Posso ajudar com mais alguma coisa?",
            ToolName: Name,
            DataType: DataType,
            Data: data);
    }
}
