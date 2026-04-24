using System.Text.RegularExpressions;
using ECommerce.AgentAPI.Application.Tools;

namespace ECommerce.AgentAPI.Infrastructure.Formatting;

/// <summary>
/// Aplicado <b>apenas</b> no <c>IntroMessage</c> do caminho "LLM sem tool" em <c>ProcessUserMessageUseCase</c>.
/// Evita exibir respostas vazias ou alucinações óbvias do modelo (ex.: <c>search_products · —</c> no lugar
/// de um produto real). Resultados de tool não passam por aqui — eles usam <c>IToolEnvelopeBuilder</c>.
/// </summary>
public static class AssistantOutputGuard
{
    private const string DefaultFailureMessage =
        "Não consegui concluir essa consulta ou ação. Tente de novo em instantes ou reformule o pedido.";

    private static readonly HashSet<string> KernelFunctionNames = BuildNameSet();

    private static HashSet<string> BuildNameSet()
    {
        var s = new HashSet<string>(StringComparer.Ordinal);
        foreach (var d in ToolRegistry.GetDefinitions())
            s.Add(d.Name);
        return s;
    }

    private static readonly Regex HallucinatedProductLine = new(
        @"^(?:\*\*)?([a-z_][\w_]*)(?:\*\*)? · —\s*$",
        RegexOptions.CultureInvariant);

    public static string EnsureUserFacing(string formatted, string? customFallback = null) =>
        string.IsNullOrWhiteSpace(formatted) || LooksLikeFunctionNameHallucination(formatted)
            ? (string.IsNullOrWhiteSpace(customFallback) ? DefaultFailureMessage : customFallback)
            : formatted!;

    private static bool LooksLikeFunctionNameHallucination(string text)
    {
        var t = text.Trim();
        if (t.Contains('\n', StringComparison.Ordinal))
            return false;

        var m = HallucinatedProductLine.Match(t);
        if (!m.Success)
            return false;

        var name = m.Groups[1].Value;
        return KernelFunctionNames.Contains(name);
    }
}
