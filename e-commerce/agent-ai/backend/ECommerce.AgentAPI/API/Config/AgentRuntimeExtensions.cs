using ECommerce.AgentAPI.Application.Options;
using ECommerce.AgentAPI.Infrastructure.Health;
using ECommerce.AgentAPI.Infrastructure.Observability;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ECommerce.AgentAPI.API.Config;

public static class AgentRuntimeExtensions
{
    public static IServiceCollection AddAgentRuntimeHardening(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AgentHostingOptions>(configuration.GetSection(AgentHostingOptions.SectionName));
        services.Configure<OtelAgentOptions>(configuration.GetSection(OtelAgentOptions.SectionName));
        services.AddRequestTimeouts();
        var mem = configuration["Memory:Provider"]?.ToLowerInvariant() ?? "volatile";
        // `live` = processo; `ready` = apto a tráfego (incl. Redis quando Memory:Provider=redis).
        var health = services.AddHealthChecks()
            .AddCheck("self", static () => HealthCheckResult.Healthy(), new[] { "live", "ready" });
        if (string.Equals(mem, "redis", StringComparison.Ordinal))
        {
            _ = health.AddCheck<RedisConnectionHealthCheck>("redis", tags: new[] { "ready" });
        }

        return services;
    }

    public static IServiceCollection AddAgentOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var o = configuration.GetSection(OtelAgentOptions.SectionName).Get<OtelAgentOptions>() ?? new OtelAgentOptions();
        if (string.IsNullOrWhiteSpace(o.OtlpEndpoint))
        {
            return services;
        }

        if (!Uri.TryCreate(o.OtlpEndpoint, UriKind.Absolute, out var otlpUri))
        {
            return services;
        }

        _ = services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    o.ServiceName,
                    serviceInstanceId: environment.EnvironmentName))
            .WithMetrics(metrics =>
            {
                if (o.EnableAspNetCore)
                {
                    metrics.AddAspNetCoreInstrumentation();
                }

                if (o.EnableHttpClient)
                {
                    metrics.AddHttpClientInstrumentation();
                }

                if (o.EnableRuntime)
                {
                    metrics.AddRuntimeInstrumentation();
                }

                metrics.AddMeter(AgentObservability.OpenTelemetryMeterName);
                metrics.AddOtlpExporter(ex => ex.Endpoint = otlpUri);
            })
            .WithTracing(tracing =>
            {
                if (o.EnableAspNetCore)
                {
                    tracing.AddAspNetCoreInstrumentation();
                }

                if (o.EnableHttpClient)
                {
                    tracing.AddHttpClientInstrumentation();
                }

                tracing
                    .AddSource(AgentObservability.ActivitySource.Name)
                    .AddOtlpExporter(ex => ex.Endpoint = otlpUri);
            });

        return services;
    }
}
