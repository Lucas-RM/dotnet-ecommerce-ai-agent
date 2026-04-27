namespace ECommerce.AgentAPI.Infrastructure.Approval;

/// <summary>
/// Resultado de uma enriquecimento de argumentos antes da aprovação. Quando <see cref="Error"/>
/// é não-nulo, o use case deve curto-circuitar o fluxo e devolver a mensagem ao usuário sem
/// persistir nada de pending.
/// </summary>
public sealed record ApprovalArgumentEnrichment(
    Dictionary<string, object> Arguments,
    string? Error);

/// <summary>
/// Pré-resolve o produto referenciado numa tool call antes de apresentar a aprovação ao usuário,
/// de forma que a pergunta ("Deseja atualizar a quantidade de X para Y?") reflita exatamente o
/// que será executado — não apenas o palpite bruto do LLM. Para <c>update_cart_item</c> e
/// <c>remove_cart_item</c> a resolução é contra o <b>carrinho atual</b>; para <c>add_cart_item</c>
/// é contra o <b>catálogo</b>. As tools que não operam sobre produto passam intactas.
/// </summary>
public interface IApprovalArgumentEnricher
{
    Task<ApprovalArgumentEnrichment> EnrichAsync(
        string toolName,
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken = default);
}

/// <inheritdoc cref="IApprovalArgumentEnricher"/>
public sealed class ApprovalArgumentEnricher : IApprovalArgumentEnricher
{
    private readonly IReadOnlyList<IToolApprovalArgumentEnrichmentStrategy> _strategies;

    public ApprovalArgumentEnricher(IEnumerable<IToolApprovalArgumentEnrichmentStrategy> strategies)
    {
        _strategies = (strategies ?? throw new ArgumentNullException(nameof(strategies))).ToArray();
    }

    public async Task<ApprovalArgumentEnrichment> EnrichAsync(
        string toolName,
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var strategy = _strategies.FirstOrDefault(s => s.CanHandle(toolName ?? string.Empty));
        if (strategy is null)
            return new ApprovalArgumentEnrichment(arguments, null);

        return await strategy
            .EnrichAsync(toolName, arguments, cancellationToken)
            .ConfigureAwait(false);
    }
}
