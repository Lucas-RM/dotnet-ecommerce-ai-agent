using System.Text.Json;
using ECommerce.AgentAPI.Application.Tools.Payloads.V1;
using ECommerce.AgentAPI.Application.Tools.Serialization;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Capabilities.Orders;

public sealed class GetOrderTool : ITool
{
    public string Name => "get_order";
    public string DataType => "Order";

    public ChatEnvelope BuildEnvelope(JsonElement? data)
    {
        var o = ToolPayloadJson.Deserialize<OrderDataV1>(data);
        var status = o?.Status;
        var intro = string.IsNullOrWhiteSpace(status)
            ? "Detalhes do pedido:"
            : $"Detalhes do pedido (status: **{status}**):";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Posso ajudar com mais alguma coisa?",
            ToolName: Name,
            DataType: DataType,
            Data: data);
    }
}
