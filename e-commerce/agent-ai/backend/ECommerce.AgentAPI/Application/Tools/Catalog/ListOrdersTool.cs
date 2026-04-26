using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Catalog;

/// <summary>
/// <c>list_orders</c> — paginação dos pedidos do usuário. Sem aprovação; envelope escolhe o intro
/// conforme total/página. Execução em <c>OrderPlugin.ListOrdersAsync</c>.
/// </summary>
public sealed class ListOrdersTool : ITool
{
    public string Name => "list_orders";

    public ChatEnvelope BuildEnvelope(JsonElement? data)
    {
        var total = EnvelopeJson.GetInt(data, "totalCount") ?? 0;
        if (total == 0)
        {
            return new ChatEnvelope(
                IntroMessage: "Você ainda não tem pedidos.",
                OutroMessage: "Posso ajudar a encontrar produtos?",
                ToolName: Name,
                DataType: "PagedOrders",
                Data: data);
        }

        var shown = EnvelopeJson.ArrayLength(data, "items");
        var intro = total == shown
            ? $"Seus pedidos ({total}):"
            : $"Mostrando {shown} de {total} pedido(s):";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Quer abrir algum pedido específico?",
            ToolName: Name,
            DataType: "PagedOrders",
            Data: data);
    }
}
