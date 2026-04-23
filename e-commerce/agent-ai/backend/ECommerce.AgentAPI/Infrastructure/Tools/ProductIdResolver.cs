using System.Net;
using System.Text.RegularExpressions;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.ECommerceClient.Dtos;
using Refit;

namespace ECommerce.AgentAPI.Infrastructure.Tools;

/// <summary>
/// Converte o <c>productId</c> vindo do LLM (às vezes número de catálogo, nome, etc.) no <see cref="Guid"/>
/// usado pela API, que trabalha com o campo <c>id</c> (UUID) de <c>search_products</c>.
/// </summary>
internal static class ProductIdResolver
{
    /// <summary>Deve alinhar com <c>ProductQueryParamsValidator.PageSize</c> na API (máx. 50).</summary>
    private const int MaxPageSize = 50;

    public static async Task<Guid?> TryResolveProductGuidAsync(
        IECommerceApi api,
        string? raw,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var t = raw.Trim();
        if (Guid.TryParse(t, out var g))
        {
            if (await ProductExistsAsync(api, g, cancellationToken).ConfigureAwait(false))
            {
                return g;
            }
            // Guid sintaticamente válido porém inexistente na loja (alucinação do modelo) — segue para heurísticas.
        }

        if (TryGetCatalogIndex(t) is { } n)
        {
            var fromCatalog = await MatchProdutoNAsync(api, n, cancellationToken).ConfigureAwait(false);
            if (fromCatalog is not null)
            {
                return fromCatalog;
            }
        }

        var r = await api
            .GetProductsAsync(new ProductQueryParams(1, MaxPageSize, null, t))
            .ConfigureAwait(false);
        var items = r.Data?.Items;
        if (items is not { Count: > 0 })
        {
            return null;
        }

        var exact = items.FirstOrDefault(p =>
            string.Equals(p.Name, t, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return exact.Id;
        }

        if (items.Count == 1)
        {
            return items[0].Id;
        }

        return null;
    }

    private static string? TryGetCatalogIndex(string t)
    {
        if (t.All(char.IsDigit))
        {
            return t;
        }

        if (t.Contains("produt", StringComparison.OrdinalIgnoreCase))
        {
            var m = Regex.Match(t, @"(?i)produt[oa]?\D*(\d+)", RegexOptions.CultureInvariant);
            if (m.Success)
            {
                return m.Groups[1].Value;
            }
        }

        var suffix = Regex.Match(t, @"(?<!\.)(\d+)$", RegexOptions.CultureInvariant);
        if (suffix.Success)
        {
            return suffix.Groups[1].Value;
        }

        return null;
    }

    private static async Task<bool> ProductExistsAsync(IECommerceApi api, Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var r = await api
                .GetProductByIdAsync(id)
                .ConfigureAwait(false);
            return r is { Success: true, Data: not null };
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    private static async Task<Guid?> MatchProdutoNAsync(IECommerceApi api, string numberPart, CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        if (string.IsNullOrEmpty(numberPart))
        {
            return null;
        }

        var want = $"Produto {numberPart}";
        // Preferir o nome exato no termo de busca (Contains) em vez de só o dígito, para reduzir colisões.
        var r = await api
            .GetProductsAsync(new ProductQueryParams(1, MaxPageSize, null, want))
            .ConfigureAwait(false);
        return r.Data?.Items?
            .FirstOrDefault(p => p.Name.Equals(want, StringComparison.OrdinalIgnoreCase))?.Id;
    }
}
