using System.Net;
using System.Text.RegularExpressions;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.ECommerceClient.Dtos;
using Refit;

namespace ECommerce.AgentAPI.Infrastructure.Tools;

/// <summary>
/// Resultado canônico de uma resolução de produto: Guid válido na API + nome amigável
/// + preço unitário no momento da resolução. Usado tanto pela pré-resolução antes da
/// aprovação (para o usuário confirmar sobre o produto certo) quanto pelos plugins.
/// </summary>
internal sealed record ResolvedProduct(Guid Id, string Name, decimal UnitPrice);

/// <summary>
/// Converte o <c>productId</c> vindo do LLM (às vezes número de catálogo, nome, etc.) no <see cref="Guid"/>
/// usado pela API, que trabalha com o campo <c>id</c> (UUID) de <c>search_products</c>.
/// <para>
/// Para operações de carrinho (<c>update_cart_item</c>/<c>remove_cart_item</c>), prefira
/// <see cref="TryResolveCartItemAsync"/>: sem heurística de índice numérico, só aceita Guid
/// ou nome que case com um item do carrinho atual — evita que um "2" qualquer (dígito que o
/// LLM extraiu da mensagem) vire silenciosamente um produto real.
/// </para>
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

    /// <summary>
    /// Resolve <paramref name="raw"/> estritamente contra o <b>carrinho atual</b> — sem a
    /// heurística "Produto N" que <see cref="TryResolveProductGuidAsync"/> aplica no catálogo.
    /// Aceita: Guid que exista no carrinho, nome exato (case-insensitive) ou <c>Contains</c>
    /// quando houver exatamente um match. Devolve <c>null</c> em qualquer outro cenário, para
    /// que o chamador possa pedir ao LLM uma nova consulta via <c>get_cart</c>.
    /// </summary>
    public static async Task<ResolvedProduct?> TryResolveCartItemAsync(
        IECommerceApi api,
        string? raw,
        CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var cart = await api.GetCartAsync().ConfigureAwait(false);
        var items = cart?.Data?.Items;
        if (items is not { Count: > 0 })
        {
            return null;
        }

        var t = raw.Trim();
        if (Guid.TryParse(t, out var g))
        {
            var byId = items.FirstOrDefault(i => i.ProductId == g);
            return byId is null ? null : new ResolvedProduct(byId.ProductId, byId.ProductName, byId.UnitPrice);
        }

        var exact = items.FirstOrDefault(i =>
            string.Equals(i.ProductName, t, StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            return new ResolvedProduct(exact.ProductId, exact.ProductName, exact.UnitPrice);
        }

        var contains = items
            .Where(i => i.ProductName.Contains(t, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (contains.Count == 1)
        {
            return new ResolvedProduct(contains[0].ProductId, contains[0].ProductName, contains[0].UnitPrice);
        }

        return null;
    }

    /// <summary>
    /// Resolve <paramref name="raw"/> no <b>catálogo</b> e devolve também o nome canônico
    /// e preço unitário (útil para enriquecer a mensagem de aprovação de <c>add_cart_item</c>).
    /// Reaproveita <see cref="TryResolveProductGuidAsync"/> para a resolução do Guid e
    /// consulta <c>get_product</c> para obter os atributos textuais.
    /// </summary>
    public static async Task<ResolvedProduct?> TryResolveCatalogProductAsync(
        IECommerceApi api,
        string? raw,
        CancellationToken cancellationToken = default)
    {
        var id = await TryResolveProductGuidAsync(api, raw, cancellationToken).ConfigureAwait(false);
        if (id is null)
        {
            return null;
        }

        try
        {
            var r = await api.GetProductByIdAsync(id.Value).ConfigureAwait(false);
            var p = r?.Data;
            if (p is null)
            {
                return null;
            }
            return new ResolvedProduct(p.Id, p.Name, p.Price);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
