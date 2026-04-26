using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools.Catalog;

/// <summary>
/// <c>clear_cart</c> — ação irreversível, sem parâmetros nem dados de retorno apresentáveis.
/// Concentra aqui: política de aprovação, texto de confirmação e envelope final da UI.
/// A execução efetiva continua em <c>CartPlugin.ClearCartAsync</c> (<c>[KernelFunction("clear_cart")]</c>).
/// </summary>
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
