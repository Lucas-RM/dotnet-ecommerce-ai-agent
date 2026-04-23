using System.Globalization;
using ECommerce.AgentAPI.Domain.Entities;

namespace ECommerce.AgentAPI.Application.Approval;

public static class ApprovalMessageBuilder
{
    public static string Build(ToolCall call)
    {
        var n = (call.Name ?? string.Empty).Trim();
        return n switch
        {
            "add_cart_item" => BuildAddCartItem(call),
            "update_cart_item" => string.Format(
                CultureInfo.GetCultureInfo("pt-BR"),
                "Deseja atualizar a quantidade de **{0}** para **{1}** unidade(s)?",
                ResolveProductLabel(call),
                Math.Max(1, ArgInt(call, "quantity", 1))),

            "remove_cart_item" => string.Format(
                CultureInfo.GetCultureInfo("pt-BR"),
                "Deseja remover **{0}** do seu carrinho?",
                ResolveProductLabel(call)),

            "clear_cart" =>
                "Tem certeza que deseja **esvaziar todo o carrinho**? Todos os itens serão removidos.",

            "checkout" => BuildCheckoutMessage(call),

            _ => $"Confirma a ação **{n}**? Responda **sim** para prosseguir ou **não** para cancelar."
        };
    }

    private static string BuildAddCartItem(ToolCall call)
    {
        var qty = Math.Max(1, ArgInt(call, "quantity", 1));
        var name = ResolveProductLabel(call);
        if (HasUsefulUnitPrice(call, out var priceDisplay))
        {
            return string.Format(
                CultureInfo.GetCultureInfo("pt-BR"),
                "Deseja adicionar **{0}** unidade(s) de **{1}** (R$ {2}) ao seu carrinho?",
                qty,
                name,
                priceDisplay);
        }

        return string.Format(
            CultureInfo.GetCultureInfo("pt-BR"),
            "Deseja adicionar **{0}** unidade(s) de **{1}** ao seu carrinho?",
            qty,
            name);
    }

    private static string ResolveProductLabel(ToolCall call)
    {
        var pn = Norm(ArgStrOptional(call, "productName"));
        var pid = Norm(ArgStrOptional(call, "productId"));

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

    private static bool HasUsefulUnitPrice(ToolCall call, out string display)
    {
        display = string.Empty;
        if (!call.Arguments.TryGetValue("unitPrice", out var o) || o is null)
            return false;
        var raw = Convert.ToString(o, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(raw) || IsPlaceholderPrice(raw))
            return false;
        display = raw;
        return true;
    }

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

    private static string BuildCheckoutMessage(ToolCall call)
    {
        if (!call.Arguments.TryGetValue("totalAmount", out var o) || o is null)
            return "Confirma a **finalização do pedido** com o valor atual do carrinho?";
        var raw = Convert.ToString(o, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(raw) || IsPlaceholderPrice(raw))
        {
            return "Confirma a **finalização do pedido** com o valor atual do carrinho?";
        }

        return string.Format(
            CultureInfo.GetCultureInfo("pt-BR"),
            "Confirma a finalização do pedido no valor de **R$ {0}**?",
            raw);
    }

    private static int ArgInt(ToolCall t, string key, int d)
    {
        if (!t.Arguments.TryGetValue(key, out var o) || o is null) return d;
        return int.TryParse(Convert.ToString(o, CultureInfo.InvariantCulture), out var v) ? v : d;
    }

    private static string ArgStrOptional(ToolCall t, string key)
    {
        if (!t.Arguments.TryGetValue(key, out var o) || o is null) return string.Empty;
        return Convert.ToString(o, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }
}
