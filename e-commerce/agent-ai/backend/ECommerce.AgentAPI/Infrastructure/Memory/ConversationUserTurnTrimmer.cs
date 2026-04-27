using ECommerce.AgentAPI.Domain.Entities;
using ECommerce.AgentAPI.Domain.Enums;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ECommerce.AgentAPI.Infrastructure.Memory;

/// <summary>
/// Lógica partilhada para limitar o histórico ao últimos N turnos de utilizador,
/// preservando o prefixo de cabeçalho (p.ex. <see cref="MessageRole.System"/> / system+developer no SK).
/// </summary>
public static class ConversationUserTurnTrimmer
{
    public static int GetContentStartForChatHistory(ChatHistory history)
    {
        var start = 0;
        while (start < history.Count &&
               (history[start].Role == AuthorRole.System || history[start].Role == AuthorRole.Developer))
            start++;
        return start;
    }

    public static int GetContentStartForDomainMessages(IReadOnlyList<ChatMessage> list)
    {
        var i = 0;
        while (i < list.Count && list[i].Role == MessageRole.System)
            i++;
        return i;
    }

    /// <summary>
    /// Se houver demasiados turnos de utilizador, devolve o intervalo a remover a partir de
    /// <paramref name="contentStartIndex"/> (inclusivo) — equivalente a <c>RemoveRange(start, count)</c>.
    /// </summary>
    public static bool TryGetRemoveRange(
        int messageCount,
        int contentStartIndex,
        Func<int, bool> isUserAt,
        int maxUserTurns,
        out int removeCount)
    {
        removeCount = 0;
        if (messageCount == 0)
            return false;

        var m = maxUserTurns < 1 ? 1 : maxUserTurns;

        var userIndices = new List<int>();
        for (var i = contentStartIndex; i < messageCount; i++)
        {
            if (isUserAt(i))
                userIndices.Add(i);
        }

        if (userIndices.Count <= m)
            return false;

        var firstUserIndexToKeep = userIndices[^m];
        var count = firstUserIndexToKeep - contentStartIndex;
        if (count <= 0)
            return false;
        removeCount = count;
        return true;
    }
}
