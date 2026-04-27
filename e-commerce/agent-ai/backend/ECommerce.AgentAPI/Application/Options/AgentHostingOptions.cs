namespace ECommerce.AgentAPI.Application.Options;

/// <summary>Limites e comportamento de alojamento: timeouts, carga, kill-switch de emergência, proxies.</summary>
public sealed class AgentHostingOptions
{
    public const string SectionName = "Agent:Hosting";

    /// <summary>Timeout de servidor para <c>POST /api/agent/chat</c> (turnos com LLM podem ser longos).</summary>
    public int ChatRequestTimeoutSeconds { get; set; } = 120;

    /// <summary>Tempo máximo para drenar pedidos após sinal de paragem (K8s rolling update).</summary>
    public int ShutdownTimeoutSeconds { get; set; } = 30;

    /// <summary><see langword="null"/> = pré-definição do Kestrel.</summary>
    public int? KestrelMaxConcurrentConnections { get; set; }

    /// <summary>Quando falso, o chat responde 503 (orquestrador/health fora; útil em incidentes).</summary>
    public bool ChatEndpointEnabled { get; set; } = true;

    /// <summary>Reenvio de <c>X-Forwarded-*</c> (ingress, reverse proxy, balanceador).</summary>
    public bool UseForwardedHeaders { get; set; }
}
