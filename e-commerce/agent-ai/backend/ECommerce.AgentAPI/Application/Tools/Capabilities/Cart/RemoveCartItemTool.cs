using System.Text.Json;
using ECommerce.AgentAPI.Application.Tools.Payloads.V1;
using ECommerce.AgentAPI.Application.Tools.Serialization;
using ECommerce.AgentAPI.Application.Tools.Shared;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Capabilities.Cart;

public sealed class RemoveCartItemTool : ITool
{
    public string Name => "remove_cart_item";
    public string DataType => "Cart";

    public bool RequiresApproval => true;

    public string BuildApprovalMessage(IReadOnlyDictionary<string, object?> arguments)
    {
        var label = ApprovalFormatting.ResolveProductLabel(arguments);
        return string.Format(
            ApprovalFormatting.Culture,
            "Deseja remover **{0}** do seu carrinho?",
            label);
    }

    public ChatEnvelope BuildEnvelope(JsonElement? data)
    {
        var c = ToolPayloadJson.Deserialize<CartDataV1>(data);
        var remaining = c?.Items?.Count ?? ToolPayloadJson.ArrayLength(data, "items");
        if (remaining == 0)
        {
            return new ChatEnvelope(
                IntroMessage: "Item removido. Seu carrinho ficou vazio.",
                OutroMessage: "Quer que eu busque outros produtos?",
                ToolName: Name,
                DataType: DataType,
                Data: data);
        }

        var total = c?.TotalPrice;
        var intro = $"Item removido. Carrinho atualizado — {remaining} item(ns), total {ToolEnvelopeText.FormatMoney(total)}:";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Posso ajudar com mais alguma coisa?",
            ToolName: Name,
            DataType: DataType,
            Data: data);
    }
}
