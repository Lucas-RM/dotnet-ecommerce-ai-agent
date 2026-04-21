using System.Collections.Concurrent;

namespace ECommerce.AgentAPI.Approval;

/// <summary>
/// Armazena, em memória e por <paramref name="sessionId"/>, a tool call pendente de aprovação.
/// </summary>
public sealed class ApprovalStateStore
{
    private readonly ConcurrentDictionary<string, PendingToolCall> _pending =
        new(StringComparer.Ordinal);

    /// <summary>Indica se existe ação aguardando confirmação para a sessão.</summary>
    public bool HasPending(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _pending.ContainsKey(sessionId);
    }

    /// <summary>Substitui ou define o estado pendente da sessão.</summary>
    public void Store(string sessionId, PendingToolCall pending)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(pending);
        _pending[sessionId] = pending;
    }

    /// <summary>Tenta obter a tool pendente sem remover.</summary>
    public bool TryGet(string sessionId, out PendingToolCall? pending)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _pending.TryGetValue(sessionId, out pending);
    }

    /// <summary>Remove o estado pendente da sessão, se existir.</summary>
    public void Clear(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        _pending.TryRemove(sessionId, out _);
    }

    /// <summary>Remove e retorna a tool pendente, se existir.</summary>
    public bool TryRemove(string sessionId, out PendingToolCall? pending)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _pending.TryRemove(sessionId, out pending);
    }
}
