using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Catalog;

/// <summary>
/// <c>remove_cart_item</c> — remove um produto específico do carrinho. Exige aprovação; a mensagem
/// inclui o rótulo do produto. Envelope adapta o intro ao carrinho que ficou vazio vs. não vazio.
/// Execução em <c>CartPlugin.RemoveCartItemAsync</c>.
/// </summary>
public sealed class RemoveCartItemTool : ITool
{
    public string Name => "remove_cart_item";

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
        var remaining = EnvelopeJson.ArrayLength(data, "items");
        if (remaining == 0)
        {
            return new ChatEnvelope(
                IntroMessage: "Item removido. Seu carrinho ficou vazio.",
                OutroMessage: "Quer que eu busque outros produtos?",
                ToolName: Name,
                DataType: "Cart",
                Data: data);
        }

        var total = EnvelopeJson.GetDecimal(data, "totalPrice");
        var intro = $"Item removido. Carrinho atualizado — {remaining} item(ns), total {EnvelopeJson.FormatMoney(total)}:";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Posso ajudar com mais alguma coisa?",
            ToolName: Name,
            DataType: "Cart",
            Data: data);
    }
}
