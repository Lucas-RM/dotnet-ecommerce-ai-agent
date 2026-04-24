using System.Globalization;
using System.Text.Json;

namespace ECommerce.AgentAPI.Application.Chat;

/// <summary>
/// Helpers internos para leitura defensiva de <see cref="JsonElement"/> pelos
/// <see cref="IToolEnvelopeBuilder"/> (preços formatados em pt-BR, campos opcionais etc.).
/// </summary>
internal static class EnvelopeJson
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public static int? GetInt(JsonElement? el, string name)
    {
        if (el is null || el.Value.ValueKind != JsonValueKind.Object)
            return null;
        if (!el.Value.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Number)
            return null;
        return prop.TryGetInt32(out var i) ? i : null;
    }

    public static decimal? GetDecimal(JsonElement? el, string name)
    {
        if (el is null || el.Value.ValueKind != JsonValueKind.Object)
            return null;
        if (!el.Value.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Number)
            return null;
        try { return prop.GetDecimal(); } catch { return null; }
    }

    public static string? GetString(JsonElement? el, string name)
    {
        if (el is null || el.Value.ValueKind != JsonValueKind.Object)
            return null;
        if (!el.Value.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.String)
            return null;
        return prop.GetString();
    }

    public static int ArrayLength(JsonElement? el, string name)
    {
        if (el is null || el.Value.ValueKind != JsonValueKind.Object)
            return 0;
        if (!el.Value.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Array)
            return 0;
        return prop.GetArrayLength();
    }

    public static string FormatMoney(decimal? value) =>
        value.HasValue ? value.Value.ToString("C", PtBr) : "—";
}
