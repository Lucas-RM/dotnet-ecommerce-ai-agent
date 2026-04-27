namespace ECommerce.AgentAPI.Application.Options;

public sealed class AgentObservabilityOptions
{
    public const string SectionName = "Agent:Observability";

    public bool EnableMetrics { get; set; } = true;

    public bool EnableTraces { get; set; } = true;
}
