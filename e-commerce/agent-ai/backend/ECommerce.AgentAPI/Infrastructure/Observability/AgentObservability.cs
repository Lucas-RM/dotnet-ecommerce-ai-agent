using System.Diagnostics;
using System.Diagnostics.Metrics;
using ECommerce.AgentAPI.Application.Abstractions;
using ECommerce.AgentAPI.Application.Capabilities;
using ECommerce.AgentAPI.Application.DTOs;
using ECommerce.AgentAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace ECommerce.AgentAPI.Infrastructure.Observability;

public sealed class AgentObservability : IAgentObservability
{
    public const string OpenTelemetryMeterName = "ECommerce.Agent";

    public static readonly ActivitySource ActivitySource = new(AgentActivityNames.Source, "1.0.0");
    private readonly IOptions<AgentObservabilityOptions> _options;
    private readonly Counter<long> _llmRounds;
    private readonly Counter<long> _toolInvocations;
    private readonly Counter<long> _approvals;
    private readonly Counter<long> _envelopeInvalid;
    private readonly Histogram<double> _llmDurationMs;
    private readonly Histogram<double> _toolDurationMs;

    public AgentObservability(IOptions<AgentObservabilityOptions> options)
    {
        _options = options;
        var meter = new Meter(OpenTelemetryMeterName, "1.0.0");

        _llmRounds = meter.CreateCounter<long>(
            "ecommerce.agent.llm.rounds",
            unit: "1",
            description: "Turnos concluídos de invocação ao LLM, por resultado.");

        _toolInvocations = meter.CreateCounter<long>(
            "ecommerce.agent.tool.invocations",
            unit: "1",
            description: "Execuções de tool no kernel, com sucesso ou falha.");

        _approvals = meter.CreateCounter<long>(
            "ecommerce.agent.approval.events",
            unit: "1",
            description: "Eventos de aprovação: pedido, confirmação, rejeição, ambíguo.");

        _envelopeInvalid = meter.CreateCounter<long>(
            "ecommerce.agent.envelope.schema_invalid",
            unit: "1",
            description: "Falhas de validação de schema do envelope de UI para dataType.");

        _llmDurationMs = meter.CreateHistogram<double>(
            "ecommerce.agent.llm.duration",
            unit: "ms",
            description: "Duração de uma rodada de LLM (GenerateAsync) até a decisão.");

        _toolDurationMs = meter.CreateHistogram<double>(
            "ecommerce.agent.tool.duration",
            unit: "ms",
            description: "Duração da invocação da função de tool no kernel.");
    }

    private bool Metrics => _options.Value.EnableMetrics;
    private bool Traces => _options.Value.EnableTraces;

    public Activity? StartChatRequestActivity(ProcessMessageCommand command)
    {
        if (!Traces)
            return null;

        var activity = ActivitySource.StartActivity(AgentActivityNames.ChatRequest, ActivityKind.Server);
        if (activity is null)
            return null;

        activity.SetTag("agent.session_id", command.SessionId);
        if (!string.IsNullOrEmpty(command.CorrelationId))
            activity.SetTag("agent.correlation_id", command.CorrelationId);
        if (!string.IsNullOrEmpty(command.Channel))
            activity.SetTag("agent.channel", command.Channel);
        if (!string.IsNullOrEmpty(command.ClientVersion))
            activity.SetTag("agent.client_version", command.ClientVersion);
        if (!string.IsNullOrEmpty(command.Locale))
            activity.SetTag("agent.locale", command.Locale);
        return activity;
    }

    public Activity? StartLlmActivity(string sessionId, string? correlationId)
    {
        if (!Traces)
            return null;

        var a = ActivitySource.StartActivity(AgentActivityNames.LlmGenerate, ActivityKind.Internal);
        if (a is not null)
        {
            a.SetTag("agent.session_id", sessionId);
            if (!string.IsNullOrEmpty(correlationId))
                a.SetTag("agent.correlation_id", correlationId);
        }

        return a;
    }

    public Activity? StartToolActivity(string toolName, string sessionId, string? correlationId)
    {
        if (!Traces)
            return null;

        var a = ActivitySource.StartActivity(AgentActivityNames.ToolExecute, ActivityKind.Internal);
        if (a is not null)
        {
            a.SetTag("agent.tool", toolName);
            a.SetTag("agent.tool.capability", ToolCapabilityResolver.Resolve(toolName).ToString());
            a.SetTag("agent.session_id", sessionId);
            if (!string.IsNullOrEmpty(correlationId))
                a.SetTag("agent.correlation_id", correlationId);
        }

        return a;
    }

    public void RecordLlmDuration(
        TimeSpan duration,
        string outcome,
        string? toolName,
        AgentCapability capability)
    {
        if (!Metrics)
            return;

        var t = Tag3(
            ("outcome", outcome),
            ("tool", toolName ?? "none"),
            ("capability", capability.ToString()));
        _llmDurationMs.Record(duration.TotalMilliseconds, t);
        _llmRounds.Add(1, t);
    }

    public void RecordToolDuration(
        string toolName,
        AgentCapability capability,
        TimeSpan duration,
        bool success,
        string? errorKind)
    {
        if (!Metrics)
            return;

        var err = string.IsNullOrEmpty(errorKind) ? "none" : errorKind!;
        var t = Tag4(
            ("agent.tool", toolName),
            ("agent.tool.capability", capability.ToString()),
            ("success", success ? "true" : "false"),
            ("error_kind", err));
        _toolDurationMs.Record(duration.TotalMilliseconds, t);
        _toolInvocations.Add(1, t);
    }

    public void RecordApproval(string eventName, string? toolName, AgentCapability capability)
    {
        if (!Metrics)
            return;

        _approvals.Add(1, Tag3(
            ("event", eventName),
            ("agent.tool", toolName ?? "none"),
            ("agent.tool.capability", capability.ToString())));
    }

    public void RecordEnvelopeInvalid(string toolName, string? dataType, string? reason)
    {
        if (!Metrics)
            return;

        var dt = string.IsNullOrEmpty(dataType) ? "none" : dataType;
        _envelopeInvalid.Add(1, Tag3(
            ("agent.tool", toolName),
            ("dataType", dt),
            ("reason", string.IsNullOrEmpty(reason) ? "unknown" : TruncateTag(reason!))));
    }

    public void AddChatSummaryTags(string sessionId, string? correlationId, string outcome)
    {
        if (!Traces)
            return;

        var a = Activity.Current;
        if (a is null)
            return;
        a.SetTag("agent.chat.outcome", outcome);
    }

    private static KeyValuePair<string, object?>[] Tag3(
        (string k, string v) a1,
        (string k, string v) a2,
        (string k, string v) a3) =>
    [
        new(a1.k, a1.v),
        new(a2.k, a2.v),
        new(a3.k, a3.v)
    ];

    private static KeyValuePair<string, object?>[] Tag4(
        (string k, string v) a1,
        (string k, string v) a2,
        (string k, string v) a3,
        (string k, string v) a4) =>
    [
        new(a1.k, a1.v),
        new(a2.k, a2.v),
        new(a3.k, a3.v),
        new(a4.k, a4.v)
    ];

    private static string TruncateTag(string s, int max = 200) =>
        s.Length <= max ? s : s[..max];
}
