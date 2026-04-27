using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.ECommerceClient.Dtos;
using Refit;

namespace ECommerce.AgentAPI.Infrastructure.Tools;

/// <summary>
/// Converte <c>orderId</c> vindo do LLM (truncado, prefixo, "último", referência vaga) no
/// <see cref="Guid"/> usado pela API, alinhado a <see cref="ProductIdResolver"/>.
/// </summary>
internal static class OrderIdResolver
{
    private const int ListPageSize = 50;

    private static readonly Regex VagueSingleOrderReference = new(
        @"(?i)\b(esse|esta|este|that|this|o\s+pedido|a\s+pedido|the\s+order|mostrar\s+esse|abrir\s+esse|ver\s+esse)\b",
        RegexOptions.CultureInvariant);

    public static async Task<Guid?> TryResolveOrderGuidAsync(
        IOrdersApi ordersApi,
        string? raw,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var normalized = Normalize(raw);
        if (IsLastOrderKeyword(normalized))
        {
            return await GetMostRecentOrderIdAsync(ordersApi, cancellationToken).ConfigureAwait(false);
        }

        if (Guid.TryParse(normalized, out var direct))
        {
            return await OrderExistsForUserAsync(ordersApi, direct, cancellationToken).ConfigureAwait(false)
                ? direct
                : null;
        }

        var hex = OnlyHexDigits(normalized);
        if (hex.Length == 32 && Guid.TryParse(hex, out var fromN))
        {
            return await OrderExistsForUserAsync(ordersApi, fromN, cancellationToken).ConfigureAwait(false)
                ? fromN
                : null;
        }

        if (hex.Length is >= 8 and < 32)
        {
            var fromPrefix = await MatchByIdPrefixAsync(ordersApi, hex, cancellationToken).ConfigureAwait(false);
            if (fromPrefix is not null)
            {
                return fromPrefix;
            }
        }

        if (TryParseUserDate(raw.Trim(), out var localDt))
        {
            var fromDate = await MatchByPlacedAtAsync(ordersApi, localDt, cancellationToken).ConfigureAwait(false);
            if (fromDate is not null)
            {
                return fromDate;
            }
        }

        if (VagueSingleOrderReference.IsMatch(raw))
        {
            return await TryResolveSingleOrderAmbiguityAsync(ordersApi, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static string Normalize(string raw)
    {
        var t = raw.Trim();
        var sb = new StringBuilder(t.Length);
        foreach (var ch in t)
        {
            if (ch is '#' or '`' or '"' or '\'')
            {
                continue;
            }

            if (ch is '\u2026' or '\u00B7') // ellipsis, middle dot
            {
                continue;
            }

            sb.Append(ch);
        }

        return sb.ToString().Trim();
    }

    private static bool IsLastOrderKeyword(string normalized)
    {
        if (normalized.Length == 0)
        {
            return false;
        }

        var n = normalized.ToLowerInvariant();
        return Regex.IsMatch(n, @"(?i)\b(último|ultimo)\b.*\bpedido\b")
               || Regex.IsMatch(n, @"(?i)\blast\b.*\border\b");
    }

    private static bool TryParseUserDate(string raw, out DateTime parsed)
    {
        if (DateTime.TryParse(
                raw,
                CultureInfo.CurrentCulture,
                DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
                out parsed))
        {
            return true;
        }

        if (DateTime.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out parsed))
        {
            return true;
        }

        return DateTime.TryParse(
            raw,
            CultureInfo.GetCultureInfo("en-US"),
            DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces,
            out parsed);
    }

    private static string OnlyHexDigits(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s)
        {
            if (Uri.IsHexDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
        }

        return sb.ToString();
    }

    private static async Task<Guid?> GetMostRecentOrderIdAsync(IOrdersApi ordersApi, CancellationToken ct)
    {
        var r = await ordersApi
            .GetOrdersAsync(new OrderQueryParams(1, 1))
            .ConfigureAwait(false);
        var first = r.Data?.Items?.FirstOrDefault();
        return first?.Id;
    }

    private static async Task<Guid?> TryResolveSingleOrderAmbiguityAsync(
        IOrdersApi ordersApi,
        CancellationToken ct)
    {
        var r = await ordersApi
            .GetOrdersAsync(new OrderQueryParams(1, 1))
            .ConfigureAwait(false);
        if (r is not { Success: true, Data: { } p } || p.TotalCount != 1)
        {
            return null;
        }

        var id = p.Items.FirstOrDefault()?.Id;
        return id == Guid.Empty ? null : id;
    }

    private static async Task<Guid?> MatchByIdPrefixAsync(
        IOrdersApi ordersApi,
        string hexPrefix,
        CancellationToken ct)
    {
        _ = ct;
        var r = await ordersApi
            .GetOrdersAsync(new OrderQueryParams(1, ListPageSize))
            .ConfigureAwait(false);
        var items = r.Data?.Items;
        if (items is not { Count: > 0 })
        {
            return null;
        }

        var matches = items
            .Where(o => o.Id.ToString("N").StartsWith(hexPrefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(o => o.PlacedAt)
            .ToList();

        if (matches.Count == 1)
        {
            return matches[0].Id;
        }

        if (matches.Count > 1)
        {
            return null;
        }

        // Cobrir listas longas: páginas seguintes só se necessário.
        var total = r.Data?.TotalCount ?? 0;
        if (total <= items.Count)
        {
            return null;
        }

        var page = 2;
        while ((page - 1) * ListPageSize < total && page <= 10)
        {
            var r2 = await ordersApi
                .GetOrdersAsync(new OrderQueryParams(page, ListPageSize))
                .ConfigureAwait(false);
            var pageItems = r2.Data?.Items;
            if (pageItems is not { Count: > 0 })
            {
                break;
            }

            matches = pageItems
                .Where(o => o.Id.ToString("N").StartsWith(hexPrefix, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(o => o.PlacedAt)
                .ToList();
            if (matches.Count == 1)
            {
                return matches[0].Id;
            }

            if (matches.Count > 1)
            {
                return null;
            }

            page++;
        }

        return null;
    }

    private static async Task<Guid?> MatchByPlacedAtAsync(
        IOrdersApi ordersApi,
        DateTime userInterpreted,
        CancellationToken ct)
    {
        _ = ct;
        var r = await ordersApi
            .GetOrdersAsync(new OrderQueryParams(1, ListPageSize))
            .ConfigureAwait(false);
        var items = r.Data?.Items;
        if (items is not { Count: > 0 })
        {
            return null;
        }

        var userUtc = userInterpreted.Kind switch
        {
            DateTimeKind.Utc => userInterpreted,
            DateTimeKind.Local => userInterpreted.ToUniversalTime(),
            _ => DateTime.SpecifyKind(userInterpreted, DateTimeKind.Local).ToUniversalTime()
        };

        const int toleranceSeconds = 120;
        var matches = items
            .Where(o => Math.Abs((o.PlacedAt.ToUniversalTime() - userUtc).TotalSeconds) <= toleranceSeconds)
            .OrderByDescending(o => o.PlacedAt)
            .ToList();

        return matches.Count switch
        {
            1 => matches[0].Id,
            > 1 => null,
            _ => null
        };
    }

    private static async Task<bool> OrderExistsForUserAsync(
        IOrdersApi ordersApi,
        Guid id,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        try
        {
            var r = await ordersApi.GetOrderByIdAsync(id).ConfigureAwait(false);
            return r is { Success: true, Data: not null };
        }
        catch (ApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}
