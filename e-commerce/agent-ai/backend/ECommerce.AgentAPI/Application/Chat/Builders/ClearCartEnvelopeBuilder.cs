using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Chat.Builders;

/// <summary>
/// Ação sem dados apresentáveis — só mensagens. <c>DataType = null</c> sinaliza ao frontend para não renderizar card.
/// </summary>
public sealed class ClearCartEnvelopeBuilder : IToolEnvelopeBuilder
{
    public string ToolName => "clear_cart";

    public ChatEnvelope Build(JsonElement? data) =>
        new ChatEnvelope(
            IntroMessage: "Carrinho esvaziado com sucesso.",
            OutroMessage: "Quer que eu busque novos produtos?",
            ToolName: ToolName,
            DataType: null,
            Data: null);
}
