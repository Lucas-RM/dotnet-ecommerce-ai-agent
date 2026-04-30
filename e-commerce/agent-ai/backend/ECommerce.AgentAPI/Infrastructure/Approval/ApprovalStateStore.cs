using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace ECommerce.AgentAPI.Infrastructure.Approval;

/// <summary>
/// Estado em memória, seguro perante concorrência (<see cref="ConcurrentDictionary{TKey,TValue}"/>),
/// mapeando <b>identificador de sessão de chat</b> → <see cref="PendingToolCall"/>,
/// enquanto o agente aguarda confirmação do utilizador para executar a tool.
/// </summary>
public sealed class ApprovalStateStore
{
    private readonly ConcurrentDictionary<string, string> _conversationToApprovalKey = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PendingToolCall> _approvalKeyToPending = new(StringComparer.Ordinal);

    public bool HasPending([NotNull] string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _conversationToApprovalKey.ContainsKey(sessionId);
    }

    public void Store([NotNull] string sessionId, PendingToolCall pending)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(pending);
        if (_conversationToApprovalKey.TryGetValue(sessionId, out var existingApprovalKey))
        {
            _approvalKeyToPending.TryRemove(existingApprovalKey, out _);
        }

        var approvalKey = BuildApprovalKey(sessionId, pending.ApprovalId);
        _conversationToApprovalKey[sessionId] = approvalKey;
        _approvalKeyToPending[approvalKey] = pending;
    }

    public bool TryGet([NotNull] string sessionId, [NotNullWhen(true)] out PendingToolCall? pending)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        pending = null;
        if (!_conversationToApprovalKey.TryGetValue(sessionId, out var approvalKey))
            return false;

        return _approvalKeyToPending.TryGetValue(approvalKey, out pending);
    }

    public bool TryGetByApprovalId(
        [NotNull] string sessionId,
        [NotNull] string approvalId,
        [NotNullWhen(true)] out PendingToolCall? pending)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(approvalId);
        return _approvalKeyToPending.TryGetValue(BuildApprovalKey(sessionId, approvalId), out pending);
    }

    public void Clear([NotNull] string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        if (_conversationToApprovalKey.TryRemove(sessionId, out var approvalKey))
        {
            _approvalKeyToPending.TryRemove(approvalKey, out _);
        }
    }

    public bool TryRemove([NotNull] string sessionId, [NotNullWhen(true)] out PendingToolCall? pending)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        pending = null;
        if (!_conversationToApprovalKey.TryRemove(sessionId, out var approvalKey))
            return false;

        return _approvalKeyToPending.TryRemove(approvalKey, out pending);
    }

    public void ClearByRawSessionId([NotNull] string rawSessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawSessionId);
        var suffix = ":" + rawSessionId;
        foreach (var conversationKey in _conversationToApprovalKey.Keys)
        {
            if (!conversationKey.EndsWith(suffix, StringComparison.Ordinal))
                continue;

            if (_conversationToApprovalKey.TryRemove(conversationKey, out var approvalKey))
            {
                _approvalKeyToPending.TryRemove(approvalKey, out _);
            }
        }
    }

    private static string BuildApprovalKey(string sessionId, string approvalId) => $"{sessionId}:{approvalId}";
}
