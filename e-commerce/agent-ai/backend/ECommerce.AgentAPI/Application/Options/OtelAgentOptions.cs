namespace ECommerce.AgentAPI.Application.Options;

/// <summary>Export OpenTelemetry (métricas/traces) para coletor OTLP — opcional até haver homolog.</summary>
public sealed class OtelAgentOptions
{
    public const string SectionName = "OpenTelemetry:Agent";

    public string? OtlpEndpoint { get; set; }

    public string ServiceName { get; set; } = "ECommerce.AgentAPI";

    public bool EnableAspNetCore { get; set; } = true;

    public bool EnableHttpClient { get; set; } = true;

    public bool EnableRuntime { get; set; } = true;
}
