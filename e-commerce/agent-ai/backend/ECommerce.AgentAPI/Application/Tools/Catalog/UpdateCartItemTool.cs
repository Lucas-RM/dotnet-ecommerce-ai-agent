using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Catalog;

/// <summary>
/// <c>update_cart_item</c> — altera a quantidade de um item do carrinho. Exige aprovação; a mensagem
/// inclui rótulo do produto e a nova quantidade. Envelope mostra o total atualizado.
/// Execução em <c>CartPlugin.UpdateCartItemAsync</c>.
/// </summary>
public sealed class UpdateCartItemTool : ITool
{
    public string Name => "update_cart_item";

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
        var total = EnvelopeJson.GetDecimal(data, "totalPrice");
        var intro = $"Quantidade atualizada. Total do carrinho: {EnvelopeJson.FormatMoney(total)}.";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Posso ajudar com mais alguma coisa?",
            ToolName: Name,
            DataType: "Cart",
            Data: data);
    }
}
