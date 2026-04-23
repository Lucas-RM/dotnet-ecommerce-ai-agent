using ECommerce.AgentAPI.Domain.Entities;

namespace ECommerce.AgentAPI.Domain.Interfaces;

public interface IMemoryService
{
    Task<List<ChatMessage>> GetHistoryAsync(string sessionId);

    Task SaveMessageAsync(ChatMessage message);

    Task PruneHistoryAsync(string sessionId, int maxTurns);

    Task ClearSessionAsync(string sessionId);
}
