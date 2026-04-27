using System.Globalization;
using System.Net;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Tools;
using Refit;

namespace ECommerce.AgentAPI.Infrastructure.Approval;

internal static class ApprovalArgumentEnrichmentSupport
{
    public static Dictionary<string, object> EnrichWithResolvedProduct(
        Dictionary<string, object> original,
        ResolvedProduct resolved)
    {
        var clone = new Dictionary<string, object>(original, StringComparer.Ordinal)
        {
            ["productId"] = resolved.Id.ToString(),
            ["productName"] = resolved.Name,
            ["unitPrice"] = resolved.UnitPrice
        };
        return clone;
    }

    public static string? ExtractProductIdentifier(Dictionary<string, object> arguments)
    {
        if (TryGetNonEmpty(arguments, "productId", out var pid))
            return pid;

        if (TryGetNonEmpty(arguments, "productName", out var pname))
            return pname;

        return null;
    }

    public static string MapApiExceptionToBusinessMessage(ApiException apiEx, bool duringCartLookup)
    {
        var detail = ECommerceApiErrorMessageReader.TryGetMessageFromApiException(apiEx);
        if (!string.IsNullOrWhiteSpace(detail))
            return detail!;

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

    private static bool TryGetNonEmpty(Dictionary<string, object> arguments, string key, out string value)
    {
        value = string.Empty;
        if (!arguments.TryGetValue(key, out var raw) || raw is null)
            return false;

        var s = Convert.ToString(raw, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        if (s.Length == 0)
            return false;

        value = s;
        return true;
    }
}
