using System.Text.Json;
using ECommerce.AgentAPI.Application.Tools.Payloads.V1;
using ECommerce.AgentAPI.Application.Tools.Serialization;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Capabilities.Orders;

/// <summary>
/// <c>list_orders</c> — domínio <b>pedidos</b>. Paginação. <c>OrderPlugin.ListOrdersAsync</c>.
/// </summary>
public sealed class ListOrdersTool : ITool
{
    public string Name => "list_orders";
    public string DataType => "PagedOrders";

    public ChatEnvelope BuildEnvelope(JsonElement? data)
    {
        var p = ToolPayloadJson.Deserialize<PagedListDataV1>(data);
        var total = p?.TotalCount ?? 0;
        if (total == 0)
        {
            return new ChatEnvelope(
                IntroMessage: "Você ainda não tem pedidos.",
                OutroMessage: "Posso ajudar a encontrar produtos?",
                ToolName: Name,
                DataType: DataType,
                Data: data);
        }

        var shown = p?.Items?.Count ?? ToolPayloadJson.ArrayLength(data, "items");
        var intro = total == shown
            ? $"Seus pedidos ({total}):"
            : $"Mostrando {shown} de {total} pedido(s):";
        var outro = BuildHumanOutro(total, shown);
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: outro,
            ToolName: Name,
            DataType: DataType,
            Data: data);
    }

    /// <summary>
    /// Texto só para o utilizador: sem nomes internos de tools nem UUIDs — o número do pedido
    /// continua visível na tabela estruturada (<c>Data</c>) no cliente.
    /// </summary>
    private static string BuildHumanOutro(int total, int shown)
    {
        var lines = new List<string>
        {
            "Quer ver os detalhes de algum? Copie o **número do pedido** da coluna **Pedido** acima e envie aqui na conversa.",
            "Também pode dizer **último pedido** para abrir o mais recente, ou a **data e hora** como aparecem na lista."
        };

        if (total > shown)
        {
            lines.Add($"Há mais resultados além desta página — é só pedir para ver os próximos ({total} no total).");
        }

        return string.Join("\n\n", lines);
    }
}
