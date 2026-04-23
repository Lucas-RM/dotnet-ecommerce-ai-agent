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
    private readonly ConcurrentDictionary<string, PendingToolCall> _pending = new(StringComparer.Ordinal);

    public bool HasPending([NotNull] string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _pending.ContainsKey(sessionId);
    }

    public void Store([NotNull] string sessionId, PendingToolCall pending)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(pending);
        _pending[sessionId] = pending;
    }

    public bool TryGet([NotNull] string sessionId, [NotNullWhen(true)] out PendingToolCall? pending)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _pending.TryGetValue(sessionId, out pending);
    }

    public void Clear([NotNull] string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        _pending.TryRemove(sessionId, out _);
    }

    public bool TryRemove([NotNull] string sessionId, [NotNullWhen(true)] out PendingToolCall? pending)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _pending.TryRemove(sessionId, out pending);
    }
}
