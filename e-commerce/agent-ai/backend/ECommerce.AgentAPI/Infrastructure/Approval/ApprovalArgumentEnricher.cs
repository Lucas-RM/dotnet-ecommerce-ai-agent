using System.Globalization;
using System.Net;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Tools;
using Refit;

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
        try
        {
            var raw = ExtractProductIdentifier(arguments);
            var resolved = await ProductIdResolver
                .TryResolveCartItemAsync(_api, raw, cancellationToken)
                .ConfigureAwait(false);
            if (resolved is null)
            {
                var action = isRemove ? "remover" : "atualizar";
                var error =
                    $"Não consegui identificar com segurança qual item você quer {action} no carrinho. Peça para eu listar seu carrinho e informe o nome completo do produto (ex.: *Produto X*).";
                return new ApprovalArgumentEnrichment(arguments, error);
            }

            return new ApprovalArgumentEnrichment(Enrich(arguments, resolved), null);
        }
        catch (ApiException apiEx)
        {
            return new ApprovalArgumentEnrichment(arguments, MapApiExceptionToBusinessMessage(apiEx, duringCartLookup: true));
        }
        catch (Exception)
        {
            const string generic =
                "Houve um problema ao consultar seu carrinho agora. Tente novamente em instantes.";
            return new ApprovalArgumentEnrichment(arguments, generic);
        }
    }

    private async Task<ApprovalArgumentEnrichment> EnrichFromCatalogAsync(
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            var raw = ExtractProductIdentifier(arguments);
            var resolved = await ProductIdResolver
                .TryResolveCatalogProductAsync(_api, raw, cancellationToken)
                .ConfigureAwait(false);
            if (resolved is null)
            {
                const string error =
                    "Não localizei esse produto na loja com segurança. Peça para eu listar opções e informe o nome completo do produto.";
                return new ApprovalArgumentEnrichment(arguments, error);
            }

            return new ApprovalArgumentEnrichment(Enrich(arguments, resolved), null);
        }
        catch (ApiException apiEx)
        {
            return new ApprovalArgumentEnrichment(arguments, MapApiExceptionToBusinessMessage(apiEx, duringCartLookup: false));
        }
        catch (Exception)
        {
            const string generic =
                "Houve um problema ao consultar os produtos da loja agora. Tente novamente em instantes.";
            return new ApprovalArgumentEnrichment(arguments, generic);
        }
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

    private static string MapApiExceptionToBusinessMessage(ApiException apiEx, bool duringCartLookup)
    {
        var detail = ECommerceApiErrorMessageReader.TryGetMessageFromApiException(apiEx);
        if (!string.IsNullOrWhiteSpace(detail))
        {
            return detail!;
        }

        return apiEx.StatusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                "Não consegui acessar sua conta da loja no momento. Tente novamente em instantes.",
            HttpStatusCode.BadRequest when duringCartLookup =>
                "Não consegui validar esse item no seu carrinho. Peça para eu listar o carrinho e tente novamente com o nome completo do produto.",
            HttpStatusCode.BadRequest =>
                "Não consegui validar esse produto na loja. Peça para eu listar opções e tente novamente com o nome completo.",
            HttpStatusCode.NotFound when duringCartLookup =>
                "Não encontrei esse item no seu carrinho. Peça para eu listar o carrinho e confirme o produto desejado.",
            HttpStatusCode.NotFound =>
                "Não encontrei esse produto na loja. Peça para eu listar opções e confirme o nome completo.",
            _ when duringCartLookup =>
                "Houve um problema ao consultar seu carrinho agora. Tente novamente em instantes.",
            _ =>
                "Houve um problema ao consultar os produtos da loja agora. Tente novamente em instantes."
        };
    }
}
