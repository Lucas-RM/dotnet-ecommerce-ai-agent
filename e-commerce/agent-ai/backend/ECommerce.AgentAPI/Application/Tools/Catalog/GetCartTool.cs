using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Catalog;

/// <summary>
/// <c>get_cart</c> — leitura do carrinho atual. Sem aprovação; envelope adapta o intro ao carrinho
/// vazio vs. preenchido (conta itens, totaliza preço). Execução em <c>CartPlugin.GetCartAsync</c>.
/// </summary>
public sealed class GetCartTool : ITool
{
    public string Name => "get_cart";

    public ChatEnvelope BuildEnvelope(JsonElement? data)
    {
        var itemCount = EnvelopeJson.ArrayLength(data, "items");
        if (itemCount == 0)
        {
            return new ChatEnvelope(
                IntroMessage: "Seu carrinho está vazio.",
                OutroMessage: "Quer que eu busque produtos na loja?",
                ToolName: Name,
                DataType: "Cart",
                Data: data);
        }

        var total = EnvelopeJson.GetDecimal(data, "totalPrice");
        var intro = $"Seu carrinho — {itemCount} item(ns), total {EnvelopeJson.FormatMoney(total)}:";
        return new ChatEnvelope(
            IntroMessage: intro,
            OutroMessage: "Deseja finalizar a compra ou continuar comprando?",
            ToolName: Name,
            DataType: "Cart",
            Data: data);
    }
}
