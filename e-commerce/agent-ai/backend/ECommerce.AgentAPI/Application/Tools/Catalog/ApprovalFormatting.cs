using System.Globalization;

namespace ECommerce.AgentAPI.Application.Tools.Catalog;

/// <summary>
/// Helpers de renderização para as mensagens de aprovação das <see cref="ITool"/>.
/// Centraliza a leitura defensiva de <c>KernelArguments</c>/<c>ToolCall.Arguments</c> — que podem
/// trazer <c>productName</c>/<c>unitPrice</c>/<c>totalAmount</c> sintetizados pelo LLM mesmo sem
/// estarem declarados no <c>[KernelFunction]</c> — e a escolha de um rótulo amigável do produto.
/// Mantido <c>internal</c> porque só interessa às implementações de <see cref="ITool"/> deste assembly.
/// </summary>
internal static class ApprovalFormatting
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static CultureInfo Culture => PtBr;

    public static int ArgInt(IReadOnlyDictionary<string, object?> args, string key, int fallback)
    {
        if (!args.TryGetValue(key, out var o) || o is null)
            return fallback;
        return int.TryParse(Convert.ToString(o, CultureInfo.InvariantCulture), out var v) ? v : fallback;
    }

    public static string ArgStr(IReadOnlyDictionary<string, object?> args, string key)
    {
        if (!args.TryGetValue(key, out var o) || o is null)
            return string.Empty;
        return Convert.ToString(o, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Escolhe um rótulo legível para um produto a partir de <c>productName</c> / <c>productId</c>.
    /// O LLM pode mandar ambos; quando só há Guid, devolve "o produto selecionado na busca".
    /// </summary>
    public static string ResolveProductLabel(IReadOnlyDictionary<string, object?> args)
    {
        var pn = Norm(ArgStr(args, "productName"));
        var pid = Norm(ArgStr(args, "productId"));

        if (LooksLikeFriendlyProductName(pn))
            return pn!;

        if (!string.IsNullOrEmpty(pn) && pn.All(char.IsDigit) && pn is { Length: >= 1 and <= 4 })
            return "Produto " + pn;

        if (Guid.TryParse(pid, out _))
            return "o produto selecionado na busca";

        if (!string.IsNullOrEmpty(pid) && pid.All(char.IsDigit) && pid.Length <= 4)
            return "Produto " + pid;

        if (LooksLikeFriendlyProductName(pid))
            return pid!;

        return "este produto";
    }

    public static bool TryGetUnitPriceDisplay(IReadOnlyDictionary<string, object?> args, out string display)
    {
        display = string.Empty;
        if (!args.TryGetValue("unitPrice", out var o) || o is null)
            return false;
        var raw = Convert.ToString(o, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(raw) || IsPlaceholderPrice(raw))
            return false;
        display = raw;
        return true;
    }

    public static bool TryGetTotalAmountDisplay(IReadOnlyDictionary<string, object?> args, out string display)
    {
        display = string.Empty;
        if (!args.TryGetValue("totalAmount", out var o) || o is null)
            return false;
        var raw = Convert.ToString(o, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(raw) || IsPlaceholderPrice(raw))
            return false;
        display = raw;
        return true;
    }

    private static bool LooksLikeFriendlyProductName(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return false;
        s = s.Trim();
        if (s.Length < 2)
            return false;
        if (s.Length <= 4 && s.All(char.IsDigit))
            return false;
        return s.Any(char.IsLetter) || s.Contains("produto", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Norm(string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static bool IsPlaceholderPrice(string raw)
    {
        var t = raw.Trim();
        if (t.Length is 0)
            return true;
        if (t is "n/d" or "N/D" or "?" or "x")
            return true;
        if (t is "-" or "–" or "—" or "…")
            return true;
        return t.All(c => c is '—' or '-' or '–' or '…' or ' ' or '\u00a0');
    }
}
