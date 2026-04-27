using System.Text.Json;
using ECommerce.AgentAPI.Application.Tools.Payloads.V1;
using ECommerce.AgentAPI.Application.Tools.Serialization;
using ECommerce.AgentAPI.Application.Tools.Shared;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Capabilities.Cart;

/// <summary>
/// <c>get_cart</c> — domínio <b>carrinho</b>. Leitura do carrinho. Execução em
/// <c>CartPlugin.GetCartAsync</c>.
/// </summary>
public sealed class GetCartTool : ITool
{
    public string Name => "get_cart";
    public string DataType => "Cart";

    public ChatEnvelope BuildEnvelope(JsonElement? data)
    {
        var c = ToolPayloadJson.Deserialize<CartDataV1>(data);
        var itemCount = c?.Items?.Count ?? ToolPayloadJson.ArrayLength(data, "items");
        if (itemCount == 0)
        {
            return new ChatEnvelope(
                IntroMessage: "Seu carrinho está vazio.",
                OutroMessage: "Quer que eu busque produtos na loja?",
                ToolName: Name,
                DataType: DataType,
                Data: data);
        }

        var total = c?.TotalPrice;
        var intro = $"Seu carrinho — {itemCount} item(ns), total {ToolEnvelopeText.FormatMoney(total)}:";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Deseja finalizar a compra ou continuar comprando?",
            ToolName: Name,
            DataType: DataType,
            Data: data);
    }
}
