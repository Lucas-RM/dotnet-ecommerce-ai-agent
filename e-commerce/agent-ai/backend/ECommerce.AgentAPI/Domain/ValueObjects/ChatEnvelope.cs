using System.Text.Json;

namespace ECommerce.AgentAPI.Domain.ValueObjects;

/// <summary>
/// Envelope padronizado que cada tool entrega à camada de apresentação.
/// Estrutura: mensagem de início (texto) → dados estruturados → mensagem de fim (texto).
/// Extensível: novas tools apenas implementam <c>IToolEnvelopeBuilder</c> e devolvem um <see cref="ChatEnvelope"/>.
/// </summary>
public sealed record ChatEnvelope(
    string? IntroMessage,
    string? OutroMessage,
    string? ToolName,
    string? DataType,
    JsonElement? Data)
{
    /// <summary>Envelope apenas com texto (sem tool/dados). Usado em respostas conversacionais, erros e aprovações.</summary>
    public static ChatEnvelope TextOnly(string? intro, string? outro = null) =>
        new(intro, outro, ToolName: null, DataType: null, Data: null);
}
