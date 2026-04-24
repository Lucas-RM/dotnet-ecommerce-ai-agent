using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Chat;

/// <summary>
/// Contrato de Strategy por tool: recebe os dados crus (já desembrulhados do envelope da loja)
/// e devolve um <see cref="ChatEnvelope"/> com intro/outro fixos + <c>dataType</c> lógico para a UI.
/// Adicionar tool nova = nova classe implementando esta interface (auto-registrada via scan).
/// </summary>
public interface IToolEnvelopeBuilder
{
    /// <summary>Nome da tool Semantic Kernel (ex.: <c>search_products</c>) que este builder atende.</summary>
    string ToolName { get; }

    /// <summary>Monta o envelope com base nos dados. <paramref name="data"/> pode ser <c>null</c> (ex.: <c>clear_cart</c>).</summary>
    ChatEnvelope Build(JsonElement? data);
}
