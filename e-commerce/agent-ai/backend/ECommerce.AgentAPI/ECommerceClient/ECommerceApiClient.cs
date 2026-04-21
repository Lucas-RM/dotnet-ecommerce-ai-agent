using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Refit;

namespace ECommerce.AgentAPI.ECommerceClient;

/// <summary>
/// Registro do <see cref="IECommerceApi"/> via Refit, Polly (retry exponencial + circuit breaker)
/// e handler que repassa o JWT do e-commerce em cada request.
/// </summary>
public static class ECommerceApiClient
{
    /// <summary>
    /// Registra <see cref="IECommerceApi"/> com Refit + Polly:
    /// retry (quantidade em <c>ECommerceApi:RetryCount</c>) com backoff exponencial,
    /// circuit breaker 5 falhas / 30s em erros transitórios HTTP,
    /// e <see cref="ECommerceApiAuthorizationHandler"/> para Bearer no outbound.
    /// </summary>
    public static IServiceCollection AddECommerceApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<ECommerceApiAuthorizationHandler>();

        var baseUrl = configuration["ECommerceApi:BaseUrl"]
            ?? throw new InvalidOperationException("Configuração 'ECommerceApi:BaseUrl' ausente.");
        var timeoutSeconds = configuration.GetValue("ECommerceApi:TimeoutSeconds", 10);
        var retryCount = configuration.GetValue("ECommerceApi:RetryCount", 2);

        services
            .AddRefitClient<IECommerceApi>()
            .ConfigureHttpClient(client =>
            {
                client.BaseAddress = new Uri(NormalizeBaseUrl(baseUrl));
                client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            })
            .AddHttpMessageHandler<ECommerceApiAuthorizationHandler>()
            .AddPolicyHandler(CreateCircuitBreakerPolicy())
            .AddPolicyHandler(CreateRetryPolicy(retryCount));

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(int retryCount)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromMilliseconds(300 * Math.Pow(2, retryAttempt - 1)));
    }

    private static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 5, durationOfBreak: TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Refit concatena o caminho do <see cref="HttpClient.BaseAddress"/> com a rota (ex.: <c>/cart</c>).
    /// Se a base terminar com <c>/</c> e a rota começar com <c>/</c>, o resultado vira <c>.../v1//cart</c> (404).
    /// Por isso a base fica sem barra no fim (ex.: <c>http://host/api/v1</c>).
    /// </summary>
    private static string NormalizeBaseUrl(string baseUrl)
    {
        return baseUrl.TrimEnd('/');
    }
}
