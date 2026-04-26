using System.Text.RegularExpressions;
using ECommerce.AgentAPI.Domain.Entities;
using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Domain.Interfaces;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.Approval;

public sealed class ToolApprovalServiceAdapter : IToolApprovalService
{
    private static readonly Regex WordBoundarySim = new(
        @"\b(sim|sí|ok|okay|pode|confirma|confirmo|aceito|claro|dale|fechado|manda|isso)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex WordBoundaryNao = new(
        @"\b(não|nao|negativo|cancela|cancelar)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly ToolApprovalService _inner;

    public ToolApprovalServiceAdapter(ToolApprovalService inner) => _inner = inner;

    public bool RequiresApproval(string toolName) => _inner.RequiresApproval(toolName);

    public Task StorePendingAsync(PendingApproval pending)
    {
        var ptc = ToPendingToolCall(pending);
        _inner.StorePending(pending.SessionId, ptc);
        return Task.CompletedTask;
    }

    public Task<PendingApproval?> GetPendingAsync(string sessionId) =>
        Task.FromResult(
            _inner.TryGetPending(sessionId, out var p) && p is not null
                ? ToDomain(sessionId, p)
                : null);

    public Task ClearPendingAsync(string sessionId)
    {
        _inner.ClearPending(sessionId);
        return Task.CompletedTask;
    }

    public ApprovalClassification ClassifyUserResponse(string message)
    {
        var t = message.Trim();
        if (string.IsNullOrEmpty(t))
            return ApprovalClassification.Ambiguous;
        if (t.Equals("s", StringComparison.OrdinalIgnoreCase))
            return ApprovalClassification.Confirmed;
        if (WordBoundaryNao.IsMatch(t) || t.Contains("deixa quieto", StringComparison.OrdinalIgnoreCase)
            || t.Contains("esquece", StringComparison.OrdinalIgnoreCase)
            || t.Contains("melhor não", StringComparison.OrdinalIgnoreCase))
            return ApprovalClassification.Denied;
        if (WordBoundarySim.IsMatch(t))
            return ApprovalClassification.Confirmed;
        return ApprovalClassification.Ambiguous;
    }

    private static PendingToolCall ToPendingToolCall(PendingApproval p) =>
        new()
        {
            FunctionName = p.ToolCall.Name,
            Arguments = ToKernelArguments(p.ToolCall.Arguments),
            ApprovalMessage = p.ApprovalMessage,
            StoredAt = new DateTimeOffset(p.CreatedAt, TimeSpan.Zero)
        };

    private static PendingApproval ToDomain(string sessionId, PendingToolCall s) =>
        new()
        {
            SessionId = sessionId,
            ApprovalMessage = s.ApprovalMessage,
            CreatedAt = s.StoredAt.UtcDateTime,
            ToolCall = new ToolCall
            {
                Name = s.FunctionName,
                SessionId = sessionId,
                Arguments = ToDictionary(s.Arguments)
            }
        };

    private static Dictionary<string, object> ToDictionary(KernelArguments a)
    {
        var d = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var kv in a)
        {
            if (kv.Value is { } o)
                d[kv.Key] = o;
        }

        return d;
    }

    private static KernelArguments ToKernelArguments(Dictionary<string, object> a)
    {
        var k = new KernelArguments();
        foreach (var kv in a)
        {
            k[kv.Key] = kv.Value;
        }

        return k;
    }
}
