using ECommerce.AgentAPI.API.Security;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace ECommerce.AgentAPI.ECommerceClient;

/// <summary>
/// <c>DelegatingHandler</c> nomeado (tipo <see cref="ECommerceApiAuthorizationHandler"/>) que,
/// em cada <see cref="HttpContext"/> da requisição ao Agent, copia o JWT do e-commerce para as
/// chamadas <strong>outbound</strong> ao Refit: <c>Authorization: Bearer {token}</c> extraído de
/// <see cref="HttpContext.Request"/>.<see cref="HttpRequest.Headers"/> no pedido de entrada.
/// </summary>
/// <remarks>Sem <see cref="HttpContext"/> (ex.: pedidos fora de um escopo HTTP) o header não é definido.</remarks>
public sealed class ECommerceApiAuthorizationHandler : DelegatingHandler
{
    private const string Bearer = "Bearer";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public ECommerceApiAuthorizationHandler(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (BearerTokenProvider.TryGetFromRequest(_httpContextAccessor.HttpContext?.Request, out var token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(Bearer, token);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
