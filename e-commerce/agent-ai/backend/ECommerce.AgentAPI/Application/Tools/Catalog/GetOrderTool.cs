using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Catalog;

/// <summary>
/// <c>get_order</c> — detalhes de um pedido. Sem aprovação; envelope opcionalmente inclui o status
/// no intro. Execução em <c>OrderPlugin.GetOrderByIdAsync</c>.
/// </summary>
public sealed class GetOrderTool : ITool
{
    public string Name => "get_order";

    public ChatEnvelope BuildEnvelope(JsonElement? data)
    {
        var status = EnvelopeJson.GetString(data, "status");
        var intro = string.IsNullOrWhiteSpace(status)
            ? "Detalhes do pedido:"
            : $"Detalhes do pedido (status: **{status}**):";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Posso ajudar com mais alguma coisa?",
            ToolName: Name,
            DataType: "Order",
            Data: data);
    }
}
