using System.Globalization;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Tools;

namespace ECommerce.AgentAPI.Infrastructure.Approval;

/// <summary>
/// Resultado de uma enriquecimento de argumentos antes da aprovação. Quando <see cref="Error"/>
/// é não-nulo, o use case deve curto-circuitar o fluxo e devolver a mensagem ao usuário sem
/// persistir nada de pending — a ideia é pedir ao LLM (via histórico) uma nova busca/get_cart.
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
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    private readonly IECommerceApi _api;

    public ApprovalArgumentEnricher(IECommerceApi api)
    {
        _api = api;
    }

    public Task<ApprovalArgumentEnrichment> EnrichAsync(
        string toolName,
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        return (toolName ?? string.Empty) switch
        {
            "update_cart_item" => EnrichFromCartAsync(arguments, isRemove: false, cancellationToken),
            "remove_cart_item" => EnrichFromCartAsync(arguments, isRemove: true, cancellationToken),
            "add_cart_item" => EnrichFromCatalogAsync(arguments, cancellationToken),
            _ => Task.FromResult(new ApprovalArgumentEnrichment(arguments, null))
        };
    }

    private async Task<ApprovalArgumentEnrichment> EnrichFromCartAsync(
        Dictionary<string, object> arguments,
        bool isRemove,
        CancellationToken cancellationToken)
    {
        var raw = ExtractProductIdentifier(arguments);
        var resolved = await ProductIdResolver
            .TryResolveCartItemAsync(_api, raw, cancellationToken)
            .ConfigureAwait(false);
        if (resolved is null)
        {
            var action = isRemove ? "remover" : "atualizar";
            var error =
                $"Não consegui identificar com certeza qual item você quer {action} no carrinho. Me diga o nome exato do produto (ex.: *Produto Teste*) ou peça a lista do carrinho.";
            return new ApprovalArgumentEnrichment(arguments, error);
        }

        return new ApprovalArgumentEnrichment(Enrich(arguments, resolved), null);
    }

    private async Task<ApprovalArgumentEnrichment> EnrichFromCatalogAsync(
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken)
    {
        var raw = ExtractProductIdentifier(arguments);
        var resolved = await ProductIdResolver
            .TryResolveCatalogProductAsync(_api, raw, cancellationToken)
            .ConfigureAwait(false);
        if (resolved is null)
        {
            const string error =
                "Não localizei esse produto na loja. Peça uma nova busca (search_products) pelo nome e tente de novo.";
            return new ApprovalArgumentEnrichment(arguments, error);
        }

        return new ApprovalArgumentEnrichment(Enrich(arguments, resolved), null);
    }

    /// <summary>
    /// Sobrescreve <c>productId</c>/<c>productName</c>/<c>unitPrice</c> com os valores canônicos do
    /// produto resolvido. Demais chaves passadas pelo LLM são preservadas (<c>quantity</c>, p.ex.).
    /// </summary>
    private static Dictionary<string, object> Enrich(
        Dictionary<string, object> original,
        ResolvedProduct resolved)
    {
        var clone = new Dictionary<string, object>(original, StringComparer.Ordinal)
        {
            ["productId"] = resolved.Id.ToString(),
            ["productName"] = resolved.Name,
            ["unitPrice"] = resolved.UnitPrice.ToString("0.00", PtBr)
        };
        return clone;
    }

    private static string? ExtractProductIdentifier(Dictionary<string, object> arguments)
    {
        if (TryGetNonEmpty(arguments, "productId", out var pid))
        {
            return pid;
        }
        if (TryGetNonEmpty(arguments, "productName", out var pn))
        {
            return pn;
        }
        return null;
    }

    private static bool TryGetNonEmpty(Dictionary<string, object> arguments, string key, out string value)
    {
        value = string.Empty;
        if (!arguments.TryGetValue(key, out var raw) || raw is null)
        {
            return false;
        }
        var s = Convert.ToString(raw, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        if (s.Length == 0)
        {
            return false;
        }
        value = s;
        return true;
    }
}
