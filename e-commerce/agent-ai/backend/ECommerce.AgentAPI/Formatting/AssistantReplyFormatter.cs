using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ECommerce.AgentAPI.Formatting;

/// <summary>
/// Converte respostas que venham como JSON (tools ou modelo colando JSON) em texto legível em português.
/// </summary>
public static class AssistantReplyFormatter
{
    private static readonly Regex CodeFence = new(
        @"^```(?:json)?\s*([\s\S]*?)\s*```\s*$",
        RegexOptions.Multiline | RegexOptions.CultureInvariant);

    /// <summary>Idempotente para texto já natural (sem JSON).</summary>
    public static string ToUserFriendly(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var text = raw.Trim();

        if (CodeFence.IsMatch(text))
        {
            var m = CodeFence.Match(text);
            if (m.Success && m.Groups.Count > 1)
                text = m.Groups[1].Value.Trim();
        }

        if (LooksLikeJsonObjectOrArray(text))
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                return FormatElement(doc.RootElement).Trim();
            }
            catch (JsonException)
            {
                // continua
            }
        }

        var slice = TryExtractOuterJsonObject(text);
        if (slice != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(slice);
                var formatted = FormatElement(doc.RootElement).Trim();
                var idx = text.IndexOf(slice, StringComparison.Ordinal);
                var prefix = idx > 0 ? text[..idx].Trim() : string.Empty;
                var suffix = idx >= 0 && idx + slice.Length < text.Length
                    ? text[(idx + slice.Length)..].Trim()
                    : string.Empty;

                return string.Join("\n\n", new[] { prefix, formatted, suffix }.Where(s => !string.IsNullOrWhiteSpace(s)));
            }
            catch (JsonException)
            {
                // ignorar
            }
        }

        return raw.Trim();
    }

    private static bool LooksLikeJsonObjectOrArray(string t)
    {
        if (t.Length < 2)
            return false;
        var c = t[0];
        return c is '{' or '[';
    }

    /// <summary>Tenta isolar um único objeto JSON quando há texto antes/depois.</summary>
    private static string? TryExtractOuterJsonObject(string text)
    {
        var start = text.IndexOf('{');
        if (start < 0)
            return null;

        var depth = 0;
        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                    return text[start..(i + 1)];
            }
        }

        return null;
    }

    private static string FormatElement(JsonElement e) =>
        e.ValueKind switch
        {
            JsonValueKind.Object => FormatObject(e),
            JsonValueKind.Array => FormatArray(e),
            JsonValueKind.String => e.GetString() ?? string.Empty,
            JsonValueKind.Number => e.TryGetInt64(out var l)
                ? l.ToString(CultureInfo.InvariantCulture)
                : e.GetRawText(),
            JsonValueKind.True => "sim",
            JsonValueKind.False => "não",
            JsonValueKind.Null => string.Empty,
            _ => e.GetRawText()
        };

    private static string FormatObject(JsonElement o)
    {
        if (HasApiEnvelopeShape(o))
            return FormatApiEnvelope(o);
        if (IsPagedShape(o))
            return FormatPagedCollection(o);
        if (IsCartShape(o))
            return FormatCart(o);
        if (IsOrderDetailShape(o))
            return FormatOrderDetail(o);
        if (LooksLikeProductRow(o))
            return FormatProductRow(o);
        if (LooksLikeOrderSummaryRow(o))
            return FormatOrderSummaryRow(o);

        return FormatGenericObject(o);
    }

    private static bool HasApiEnvelopeShape(JsonElement o) =>
        o.TryGetProperty("success", out _);

    private static string FormatApiEnvelope(JsonElement o)
    {
        var sb = new StringBuilder();

        if (o.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
        {
            var msg = msgEl.GetString();
            if (!string.IsNullOrWhiteSpace(msg))
                sb.AppendLine(msg.Trim());
        }

        if (o.TryGetProperty("errors", out var errs) && errs.ValueKind == JsonValueKind.Array)
        {
            foreach (var er in errs.EnumerateArray())
            {
                if (er.ValueKind == JsonValueKind.String)
                    sb.AppendLine("• " + er.GetString());
            }
        }

        if (o.TryGetProperty("data", out var data))
        {
            if (data.ValueKind is JsonValueKind.Null)
                return sb.ToString().TrimEnd();

            var inner = FormatElement(data);
            if (!string.IsNullOrWhiteSpace(inner))
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.Append(inner);
            }
        }
        else if (sb.Length == 0)
        {
            return FormatGenericObject(o);
        }

        return sb.ToString().TrimEnd();
    }

    private static bool IsPagedShape(JsonElement o) =>
        o.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array
        && o.TryGetProperty("totalCount", out _);

    private static string FormatPagedCollection(JsonElement o)
    {
        var sb = new StringBuilder();
        var page = GetInt(o, "page") ?? 1;
        var pageSize = GetInt(o, "pageSize") ?? 10;
        var total = GetInt(o, "totalCount") ?? 0;
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0;

        if (total > 0)
            sb.AppendLine($"**Resultados** — página {page} de {Math.Max(1, totalPages)} ({total} no total).");
        else
            sb.AppendLine("**Resultados** — nenhum item encontrado.");

        if (!o.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return sb.ToString().TrimEnd();

        var i = 0;
        foreach (var item in items.EnumerateArray())
        {
            i++;
            if (LooksLikeProductRow(item))
                sb.AppendLine($"{i}. {FormatProductRow(item)}");
            else if (LooksLikeOrderSummaryRow(item))
                sb.AppendLine($"{i}. {FormatOrderSummaryRow(item)}");
            else
                sb.AppendLine($"{i}. {FormatElement(item)}");
        }

        return sb.ToString().TrimEnd();
    }

    private static bool IsCartShape(JsonElement o) =>
        o.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array
        && o.TryGetProperty("totalPrice", out _);

    private static string FormatCart(JsonElement o)
    {
        var sb = new StringBuilder();
        sb.AppendLine("**Seu carrinho**");

        if (!o.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0)
        {
            sb.AppendLine("Carrinho vazio.");
            return sb.ToString().TrimEnd();
        }

        foreach (var line in items.EnumerateArray())
        {
            if (line.ValueKind != JsonValueKind.Object)
                continue;
            var name = GetString(line, "productName") ?? "Item";
            var qty = GetInt(line, "quantity") ?? 0;
            var unit = GetDecimal(line, "unitPrice");
            var sub = GetDecimal(line, "subtotal");
            sb.AppendLine($"• **{name}** — {qty} × {FormatMoney(unit)} = {FormatMoney(sub)}");
        }

        if (o.TryGetProperty("totalPrice", out var tp) && tp.ValueKind == JsonValueKind.Number)
            sb.AppendLine($"**Total:** {FormatMoney(tp.GetDecimal())}");

        return sb.ToString().TrimEnd();
    }

    private static bool IsOrderDetailShape(JsonElement o) =>
        o.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array
        && (o.TryGetProperty("placedAt", out _) || o.TryGetProperty("totalAmount", out _))
        && o.TryGetProperty("status", out _);

    private static string FormatOrderDetail(JsonElement o)
    {
        var sb = new StringBuilder();
        var id = GetString(o, "id");
        if (!string.IsNullOrEmpty(id))
            sb.AppendLine($"**Pedido** `{id}`");

        if (o.TryGetProperty("placedAt", out var placed) && placed.ValueKind == JsonValueKind.String)
        {
            if (DateTime.TryParse(placed.GetString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
                sb.AppendLine($"**Data:** {dt.ToLocalTime():dd/MM/yyyy HH:mm}");
        }

        if (o.TryGetProperty("status", out var st) && st.ValueKind == JsonValueKind.String)
            sb.AppendLine($"**Status:** {st.GetString()}");

        if (o.TryGetProperty("totalAmount", out var ta) && ta.ValueKind == JsonValueKind.Number)
            sb.AppendLine($"**Total:** {FormatMoney(ta.GetDecimal())}");

        if (o.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array && items.GetArrayLength() > 0)
        {
            sb.AppendLine();
            sb.AppendLine("**Itens:**");
            foreach (var line in items.EnumerateArray())
            {
                if (line.ValueKind != JsonValueKind.Object)
                    continue;
                var name = GetString(line, "productName") ?? "Produto";
                var qty = GetInt(line, "quantity") ?? 0;
                var sub = GetDecimal(line, "subtotal");
                sb.AppendLine($"• {name} — {qty} un. — {FormatMoney(sub)}");
            }
        }

        return sb.ToString().TrimEnd();
    }

    private static bool LooksLikeProductRow(JsonElement o) =>
        o.ValueKind == JsonValueKind.Object && o.TryGetProperty("name", out _);

    private static string FormatProductRow(JsonElement o)
    {
        var name = GetString(o, "name") ?? "Produto";
        var price = GetDecimal(o, "price");
        var cat = GetString(o, "category");
        var stock = GetInt(o, "stockQuantity");

        var parts = new List<string> { $"**{name}**", FormatMoney(price) };
        if (!string.IsNullOrEmpty(cat))
            parts.Add(cat!);
        if (stock.HasValue)
            parts.Add($"estoque: {stock}");

        return string.Join(" · ", parts);
    }

    private static bool LooksLikeOrderSummaryRow(JsonElement o) =>
        o.ValueKind == JsonValueKind.Object
        && o.TryGetProperty("totalAmount", out _)
        && o.TryGetProperty("status", out _);

    private static string FormatOrderSummaryRow(JsonElement o)
    {
        var id = GetString(o, "id");
        var status = GetString(o, "status");
        var total = GetDecimal(o, "totalAmount");
        var placed = GetString(o, "placedAt");
        var line = new StringBuilder();
        if (!string.IsNullOrEmpty(placed) && DateTime.TryParse(placed, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dt))
            line.Append($"{dt.ToLocalTime():dd/MM/yyyy} — ");
        line.Append($"{status ?? "?"} — {FormatMoney(total)}");
        if (!string.IsNullOrEmpty(id))
            line.Append($" (`{id}`)");
        return line.ToString();
    }

    private static string FormatArray(JsonElement arr)
    {
        var sb = new StringBuilder();
        var i = 0;
        foreach (var el in arr.EnumerateArray())
        {
            i++;
            sb.AppendLine($"{i}. {FormatElement(el)}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatGenericObject(JsonElement o)
    {
        var sb = new StringBuilder();
        foreach (var p in o.EnumerateObject())
        {
            var formatted = FormatElement(p.Value);
            sb.AppendLine($"**{HumanizeKey(p.Name)}:** {formatted}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string HumanizeKey(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;
        return char.ToUpperInvariant(name[0]) + name[1..];
    }

    private static string FormatMoney(decimal? d)
    {
        if (!d.HasValue)
            return "—";
        return d.Value.ToString("C", new CultureInfo("pt-BR"));
    }

    private static string? GetString(JsonElement o, string name)
    {
        if (!o.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.String)
            return null;
        return el.GetString();
    }

    private static int? GetInt(JsonElement o, string name)
    {
        if (!o.TryGetProperty(name, out var el))
            return null;
        return el.ValueKind switch
        {
            JsonValueKind.Number when el.TryGetInt32(out var i) => i,
            _ => null
        };
    }

    private static decimal? GetDecimal(JsonElement o, string name)
    {
        if (!o.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Number)
            return null;
        try
        {
            return el.GetDecimal();
        }
        catch
        {
            return null;
        }
    }
}
