using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace ECommerce.AgentAPI.ECommerceClient;

/// <summary>
/// Encaminha o header <c>Authorization: Bearer</c> da requisição ao Agent API
/// para cada chamada HTTP feita pelo Refit à API e-commerce.
/// </summary>
public sealed class ECommerceApiAuthorizationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ECommerceApiAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.Request.Headers.TryGetValue(HeaderNames.Authorization, out var header) == true)
        {
            var value = header.ToString();
            if (!string.IsNullOrEmpty(value))
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(value);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
