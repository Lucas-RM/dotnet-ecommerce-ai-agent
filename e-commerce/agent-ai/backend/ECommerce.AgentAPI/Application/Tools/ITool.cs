using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools;

/// <summary>
/// "Uma tool, uma classe": concentra num único lugar os três aspectos de apresentação/policy de uma tool
/// do Semantic Kernel — política de aprovação, template da mensagem de confirmação e envelope de UI.
/// A execução continua nos <c>[KernelFunction]</c> dos plugins; este contrato é apenas metadata.
/// Adicionar uma nova tool passa a ser: método no plugin + classe <see cref="ITool"/> neste assembly.
/// </summary>
public interface ITool
{
    /// <summary>Nome do <c>[KernelFunction]</c> que esta tool representa (ex.: <c>clear_cart</c>).</summary>
    string Name { get; }

    /// <summary>Se <c>true</c>, a execução é interceptada para exigir confirmação explícita do usuário.</summary>
    bool RequiresApproval => false;

    /// <summary>
    /// Tipo lógico de payload retornado para a UI (ex.: <c>Cart</c>, <c>Product</c>). <c>null</c> indica
    /// resposta textual sem card dedicado.
    /// </summary>
    string? DataType => null;

    /// <summary>
    /// Versão do contrato da tool para governança de compatibilidade.
    /// </summary>
    string Version => ToolContractVersion.Current;

    /// <summary>
    /// Mensagem mostrada ao usuário quando a confirmação é necessária. <c>null</c>/<c>""</c> faz o
    /// <see cref="ToolCatalog"/> cair no texto genérico "Confirma a ação <b>{name}</b>?".
    /// </summary>
    string? BuildApprovalMessage(IReadOnlyDictionary<string, object?> arguments) => null;

    /// <summary>
    /// Projeta o resultado bruto da tool (já desembrulhado do envelope <c>{success,data}</c> da API)
    /// num <see cref="ChatEnvelope"/> pronto para a UI. A forma de <c>data</c> alinha a
    /// <c>Application.Tools.Payloads.V1</c> (versão 1) e a deserialização central
    /// <c>Application.Tools.Serialization.ToolPayloadJson</c> (ISSUE-08).
    /// </summary>
    ChatEnvelope BuildEnvelope(JsonElement? data);
}
