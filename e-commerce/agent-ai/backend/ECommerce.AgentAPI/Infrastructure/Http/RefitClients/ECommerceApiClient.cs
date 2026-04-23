using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using Refit;

namespace ECommerce.AgentAPI.ECommerceClient;

/// <summary>
/// Extensão de <see cref="IServiceCollection"/> que regista
/// <see cref="IECommerceApi"/> (Refit) com políticas Polly via
/// <c>Microsoft.Extensions.Http.Polly</c> (<b>AddPolicyHandler</b>) e
/// o <see cref="ECommerceApiAuthorizationHandler"/>, de modo que o JWT do e-commerce
/// acompanhe cada request HTTP.
/// </summary>
public static class ECommerceApiClient
{
    private const int CircuitBreakAfterFailures = 5;
    private const int CircuitOpenSeconds = 30;
    private const int RetryBaseDelayMs = 300;
    private const int DefaultTimeoutSeconds = 10;
    private const int DefaultRetryCount = 2;

    /// <summary>
    /// <list type="bullet">
    ///   <item><b>Refit</b> – cliente tipado <see cref="IECommerceApi" /> (pacote <c>Refit.HttpClientFactory</c>).</item>
    ///   <item>
    ///     <b>Polly</b> (Microsoft.Extensions.Http.Polly) – retry com backoff exponencial
    ///     (quantidade em <c>ECommerceApi:RetryCount</c>, predefinido 2) + circuit breaker
    ///     (<see cref="CircuitBreakAfterFailures" /> falhas, abertura de <see cref="CircuitOpenSeconds" />s).
    ///   </item>
    ///   <item>
    ///     <see cref="ECommerceApiAuthorizationHandler" /> (Transient) – injeta o Bearer do
    ///     <see cref="IHttpContextAccessor" /> no pedido outbound.
    ///   </item>
    /// </list>
    /// A ordem de registo das políticas faz o <b>retry</b> ser a política mais externa; o
    /// <b>circuit breaker</b> fica mais interior (próximo do transporte), padrão habitual com Polly.
    /// </summary>
    public static IServiceCollection AddECommerceApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddTransient<ECommerceApiAuthorizationHandler>();

        var baseUrl = configuration["ECommerceApi:BaseUrl"]
            ?? throw new InvalidOperationException("Configuração 'ECommerceApi:BaseUrl' ausente.");
        var timeoutSeconds = configuration.GetValue("ECommerceApi:TimeoutSeconds", DefaultTimeoutSeconds);
        var retryCount = configuration.GetValue("ECommerceApi:RetryCount", DefaultRetryCount);

        // Polly: PollyServiceCollectionExtensions (assembly Microsoft.Extensions.Http.Polly) — IHttpClientBuilder
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

    private static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(int retryCount) =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromMilliseconds(RetryBaseDelayMs * Math.Pow(2, retryAttempt - 1)));

    private static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy() =>
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: CircuitBreakAfterFailures,
                durationOfBreak: TimeSpan.FromSeconds(CircuitOpenSeconds));

    /// <summary>
    /// Refit concatena o caminho do <see cref="HttpClient.BaseAddress"/> com a rota (ex.: <c>/cart</c>).
    /// Se a base terminar com <c>/</c> e a rota começar com <c>/</c>, o resultado vira <c>.../v1//cart</c> (404).
    /// </summary>
    private static string NormalizeBaseUrl(string baseUrl) => baseUrl.TrimEnd('/');
}
