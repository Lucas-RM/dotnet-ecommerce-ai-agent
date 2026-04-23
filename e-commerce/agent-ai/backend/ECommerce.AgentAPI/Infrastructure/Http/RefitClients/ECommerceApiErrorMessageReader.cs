using System.Text.Json;
using Refit;

namespace ECommerce.AgentAPI.ECommerceClient;

/// <summary>
/// Extrai a mensagem do envelope <c>ApiResponse</c> da API e-commerce
/// (JSON com <c>message</c> e, opcionalmente, <c>errors</c>).
/// </summary>
public static class ECommerceApiErrorMessageReader
{
    public static string? TryGetMessageFromApiException(ApiException? ex)
    {
        if (ex is null)
        {
            return null;
        }

        return TryParseErrorBody(ex.Content);
    }

    public static string? TryParseErrorBody(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var t = content.Trim();
        if (t[0] is not '{' and not '[')
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(t);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            string? msg = null;
            if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String)
            {
                msg = m.GetString();
            }

            if (root.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    parts.Add(msg!);
                }

                foreach (var e in errs.EnumerateArray())
                {
                    if (e.ValueKind == JsonValueKind.String)
                    {
                        var s = e.GetString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            parts.Add(s!);
                        }
                    }
                }

                if (parts.Count > 0)
                {
                    return string.Join(" ", parts);
                }
            }

            return string.IsNullOrWhiteSpace(msg) ? null : msg;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
