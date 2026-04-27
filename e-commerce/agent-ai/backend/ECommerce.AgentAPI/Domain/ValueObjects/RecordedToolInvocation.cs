namespace ECommerce.AgentAPI.Domain.ValueObjects;

/// <summary>Resultado bruto de um plugin (Kernel) gravado após <c>await next</c> no fluxo
/// sem aprovação, para o caso de uso montar o <see cref="ChatEnvelope"/> (cards).</summary>
public sealed class RecordedToolInvocation
{
    public required string FunctionName { get; init; }
    public required string RawResult { get; init; }
}
