using System.Collections.Concurrent;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Memory;

#pragma warning disable SKEXP0050 // VolatileMemoryStore (pacote Plugins.Memory em preview)

namespace ECommerce.AgentAPI.Kernel;

/// <summary>
/// Mantém, por <paramref name="sessionId"/>, um <see cref="VolatileMemoryStore"/> e um <see cref="ChatHistory"/>
/// com janela deslizante de até <c>Agent:MaxConversationTurns</c> turnos (mensagens de usuário).
/// </summary>
public sealed class AgentMemoryStore
{
    private readonly ConcurrentDictionary<string, SessionMemory> _sessions = new(StringComparer.Ordinal);
    private readonly int _maxConversationTurns;

    public AgentMemoryStore(IConfiguration configuration)
    {
        var max = configuration.GetValue("Agent:MaxConversationTurns", 20);
        _maxConversationTurns = max < 1 ? 20 : max;
    }

    /// <summary>Histórico de chat da sessão (multi-turn).</summary>
    public ChatHistory GetOrCreate(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _sessions.GetOrAdd(sessionId, _ => new SessionMemory()).ChatHistory;
    }

    /// <summary>Armazenamento volátil em memória associado à sessão (ex.: memória semântica futura).</summary>
    public VolatileMemoryStore GetVolatileMemoryStore(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _sessions.GetOrAdd(sessionId, _ => new SessionMemory()).VolatileStore;
    }

    /// <summary>
    /// Remove turnos mais antigos quando o número de mensagens de usuário excede o limite configurado.
    /// Preserva mensagens <see cref="AuthorRole.System"/> e <see cref="AuthorRole.Developer"/> no início.
    /// </summary>
    public void TrimConversation(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        if (!_sessions.TryGetValue(sessionId, out var session))
            return;

        lock (session.Sync)
        {
            TrimChatHistoryToMaxUserTurns(session.ChatHistory, _maxConversationTurns);
        }
    }

    private sealed class SessionMemory
    {
        public VolatileMemoryStore VolatileStore { get; } = new();
        public ChatHistory ChatHistory { get; } = new();
        public object Sync { get; } = new();
    }

    private static void TrimChatHistoryToMaxUserTurns(ChatHistory history, int maxUserTurns)
    {
        if (maxUserTurns < 1 || history.Count == 0)
            return;

        var start = 0;
        while (start < history.Count &&
               (history[start].Role == AuthorRole.System || history[start].Role == AuthorRole.Developer))
            start++;

        var userIndices = new List<int>();
        for (var i = start; i < history.Count; i++)
        {
            if (history[i].Role == AuthorRole.User)
                userIndices.Add(i);
        }

        if (userIndices.Count <= maxUserTurns)
            return;

        var firstUserIndexToKeep = userIndices[^maxUserTurns];
        var removeCount = firstUserIndexToKeep - start;
        if (removeCount > 0)
            history.RemoveRange(start, removeCount);
    }
}

#pragma warning restore SKEXP0050
