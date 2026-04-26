using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Catalog;

/// <summary>
/// <c>checkout</c> — finaliza o pedido a partir do carrinho atual. Ação irreversível, exige
/// confirmação explícita. Quando o LLM sintetiza <c>totalAmount</c> nos argumentos, a mensagem
/// mostra o valor; senão, cai num texto genérico.
/// Execução em <c>OrderPlugin.CheckoutAsync</c> (<c>[KernelFunction("checkout")]</c>).
/// </summary>
public sealed class CheckoutTool : ITool
{
    public string Name => "checkout";

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
        var total = EnvelopeJson.GetDecimal(data, "totalAmount");
        var intro = $"Pedido concluído! Total: {EnvelopeJson.FormatMoney(total)}.";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Obrigado pela compra! Posso buscar novos produtos?",
            ToolName: Name,
            DataType: "Order",
            Data: data);
    }
}
