using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Capabilities.Cart;

/// <summary>Domínio <b>carrinho</b>. <c>clear_cart</c> em <c>CartPlugin.ClearCartAsync</c>.</summary>
public sealed class ClearCartTool : ITool
{
    public string Name => "clear_cart";

    public bool RequiresApproval => true;

    public string BuildApprovalMessage(IReadOnlyDictionary<string, object?> arguments) =>
        "Tem certeza que deseja **esvaziar todo o carrinho**? Todos os itens serão removidos.";

    public ChatEnvelope BuildEnvelope(JsonElement? data) =>
        new(
            IntroMessage: "Carrinho esvaziado com sucesso.",
            OutroMessage: "Quer que eu busque novos produtos?",
            ToolName: Name,
            DataType: null,
            Data: null);
}
