using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools;

/// <summary>
/// Índice das <see cref="ITool"/> registradas no DI e ponto único de consulta para policy de aprovação,
/// mensagem de confirmação e envelope de UI. Depois da migração do Passo 4 (diagnóstico), deixou de haver
/// caminhos legacy — quando uma tool não está no catálogo, devolvem-se fallbacks textuais mínimos em vez
/// de delegar para <c>IToolEnvelopeBuilder</c> / <c>ApprovalMessageBuilder</c>.
/// </summary>
public sealed class ToolCatalog
{
    private readonly IReadOnlyDictionary<string, ITool> _byName;

    public ToolCatalog(IEnumerable<ITool> tools)
    {
        ArgumentNullException.ThrowIfNull(tools);
        _byName = tools
            .GroupBy(t => t.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);
    }

    public ITool? Get(string name) =>
        !string.IsNullOrEmpty(name) && _byName.TryGetValue(name, out var t) ? t : null;

    public bool Contains(string name) => Get(name) is not null;

    public bool RequiresApproval(string name) => Get(name)?.RequiresApproval ?? false;

    /// <summary>
    /// Monta a mensagem de aprovação via <see cref="ITool.BuildApprovalMessage"/>. Se a tool não existir
    /// ou devolver <c>null</c>/vazio, volta a um texto genérico sem expor nomes internos.
    /// </summary>
    public string BuildApprovalMessage(string name, IReadOnlyDictionary<string, object?> arguments)
    {
        var tool = Get(name);
        var msg = tool?.BuildApprovalMessage(arguments);
        if (!string.IsNullOrEmpty(msg))
            return msg;

        return "Confirma esta ação? Responda **sim** para prosseguir ou **não** para cancelar.";
    }

    /// <summary>
    /// Produz o <see cref="ChatEnvelope"/> da tool. Se não houver <see cref="ITool"/> para o nome,
    /// devolve um envelope "cru" (sem intro/outro/dataType) carregando os dados para a UI cair no ramo
    /// textual — mesmo comportamento do antigo <c>ToolEnvelopeRegistry</c>.
    /// </summary>
    public ChatEnvelope BuildEnvelope(string name, JsonElement? data)
    {
        var tool = Get(name);
        if (tool is not null)
            return tool.BuildEnvelope(data);

        return new ChatEnvelope(
            IntroMessage: null,
            OutroMessage: null,
            ToolName: name,
            DataType: null,
            Data: data);
    }
}
