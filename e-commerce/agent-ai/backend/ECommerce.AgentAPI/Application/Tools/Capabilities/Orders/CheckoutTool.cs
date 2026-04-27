using System.Text.Json;
using ECommerce.AgentAPI.Application.Tools.Payloads.V1;
using ECommerce.AgentAPI.Application.Tools.Serialization;
using ECommerce.AgentAPI.Application.Tools.Shared;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Capabilities.Orders;

/// <summary><c>checkout</c> — <c>OrderPlugin.CheckoutAsync</c>. Domínio <b>pedidos</b> / fecho.</summary>
public sealed class CheckoutTool : ITool
{
    public string Name => "checkout";
    public string DataType => "Order";

    public bool RequiresApproval => true;

    public string BuildApprovalMessage(IReadOnlyDictionary<string, object?> arguments)
    {
        if (!ApprovalFormatting.TryGetTotalAmountDisplay(arguments, out var total))
            return "Confirma a **finalização do pedido** com o valor atual do carrinho?";

        return string.Format(
            ApprovalFormatting.Culture,
            "Confirma a finalização do pedido no valor de **R$ {0}**?",
            total);
    }

    public ChatEnvelope BuildEnvelope(JsonElement? data)
    {
        var o = ToolPayloadJson.Deserialize<OrderDataV1>(data);
        var total = o?.TotalAmount;
        var intro = $"Pedido concluído! Total: {ToolEnvelopeText.FormatMoney(total)}.";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Obrigado pela compra! Posso buscar novos produtos?",
            ToolName: Name,
            DataType: DataType,
            Data: data);
    }
}
