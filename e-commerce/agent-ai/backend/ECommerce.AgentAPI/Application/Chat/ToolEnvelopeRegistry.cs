using System.Text.Json;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Chat;

/// <summary>
/// Indexa todos os <see cref="IToolEnvelopeBuilder"/> registrados no DI por <see cref="IToolEnvelopeBuilder.ToolName"/>.
/// É o único ponto que o use-case consulta para converter um resultado de tool em <see cref="ChatEnvelope"/>.
/// </summary>
public sealed class ToolEnvelopeRegistry
{
    private readonly IReadOnlyDictionary<string, IToolEnvelopeBuilder> _byName;

    public ToolEnvelopeRegistry(IEnumerable<IToolEnvelopeBuilder> builders)
    {
        _byName = builders
            .GroupBy(b => b.ToolName, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);
    }

    /// <summary>
    /// Busca o builder da tool e produz o envelope. Se não houver builder registrado,
    /// devolve um envelope de fallback com os dados crus (UI cai no ramo "texto puro").
    /// </summary>
    public ChatEnvelope BuildFor(string toolName, JsonElement? data) =>
        _byName.TryGetValue(toolName, out var builder)
            ? builder.Build(data)
            : new ChatEnvelope(
                IntroMessage: null,
                OutroMessage: null,
                ToolName: toolName,
                DataType: null,
                Data: data);
}
