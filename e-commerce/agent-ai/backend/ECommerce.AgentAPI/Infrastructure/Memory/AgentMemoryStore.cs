using System.Collections.Concurrent;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ECommerce.AgentAPI.Infrastructure.Memory;

/// <summary>
/// Mantém, por <paramref name="sessionId"/>, um <see cref="ChatHistory"/> multi-turno (em processo).
/// A janela de turnos de utilizador é aplicada em <see cref="PruneTo"/> via
/// <see cref="ConversationUserTurnTrimmer"/> e com o limite vindo de <c>IMemoryService.PruneHistoryAsync</c>.
/// </summary>
public sealed class AgentMemoryStore
{
    private readonly ConcurrentDictionary<string, SessionMemory> _sessions = new(StringComparer.Ordinal);

    /// <summary>Histórico de chat da sessão (multi-turn).</summary>
    public ChatHistory GetOrCreate(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _sessions.GetOrAdd(sessionId, _ => new SessionMemory()).ChatHistory;
    }

    public void PruneTo(string sessionId, int maxUserTurns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        if (!_sessions.TryGetValue(sessionId, out var session))
            return;

        var m = maxUserTurns < 1 ? 1 : maxUserTurns;
        lock (session.Sync)
        {
            var history = session.ChatHistory;
            var start = ConversationUserTurnTrimmer.GetContentStartForChatHistory(history);
            if (ConversationUserTurnTrimmer.TryGetRemoveRange(
                    history.Count,
                    start,
                    i => history[i].Role == AuthorRole.User,
                    m,
                    out var removeCount))
                history.RemoveRange(start, removeCount);
        }
    }

    public void RemoveSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        _sessions.TryRemove(sessionId, out _);
    }

    private sealed class SessionMemory
    {
        public ChatHistory ChatHistory { get; } = new();
        public object Sync { get; } = new();
    }
}
