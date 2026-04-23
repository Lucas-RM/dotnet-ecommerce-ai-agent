using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace ECommerce.AgentAPI.API.Security;

/// <summary>Extrai o token Bearer do <see cref="HttpContext"/> de entrada (mesma regra que
/// <see cref="ECommerce.AgentAPI.ECommerceClient.ECommerceApiAuthorizationHandler"/>).</summary>
public static class BearerTokenProvider
{
    private const string Bearer = "Bearer";

    public static bool TryGetFromRequest([NotNullWhen(true)] HttpRequest? request, [NotNullWhen(true)] out string? token) =>
        TryGetBearerFromIncomingRequest(request, out token);

    private static bool TryGetBearerFromIncomingRequest(
        [NotNullWhen(true)] HttpRequest? request,
        [NotNullWhen(true)] out string? token)
    {
        token = null;
        if (request is null)
            return false;

        if (!request.Headers.TryGetValue(HeaderNames.Authorization, out var values))
            return false;

        var value = values.Count > 0 ? values[0]! : (string?)null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (AuthenticationHeaderValue.TryParse(value, out var header)
            && string.Equals(header.Scheme, Bearer, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(header.Parameter))
        {
            token = header.Parameter;
            return true;
        }

        const string prefix = "Bearer ";
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && value.Length > prefix.Length)
        {
            token = value[prefix.Length..].Trim();
            return !string.IsNullOrEmpty(token);
        }

        return false;
    }
}
