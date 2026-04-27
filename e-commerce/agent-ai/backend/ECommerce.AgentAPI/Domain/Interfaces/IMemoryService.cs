using ECommerce.AgentAPI.Domain.Entities;

namespace ECommerce.AgentAPI.Domain.Interfaces;

/// <summary>Abstracção do histórico de conversa por <c>sessionId</c> (volátil em memória ou Redis).</summary>
public interface IMemoryService
{
    /// <summary>
    /// Histórico para envio ao LLM. Em implementações voláteis baseadas em <c>ChatHistory</c>, as leituras
    /// podem ser projeções sem identidade/timestamps estáveis — ver documentação da implementação.
    /// </summary>
    Task<List<ChatMessage>> GetHistoryAsync(string sessionId);

    Task SaveMessageAsync(ChatMessage message);

    Task PruneHistoryAsync(string sessionId, int maxTurns);

    /// <summary>Remove histórico persistido e estado associado à sessão (ex.: nova conversa no cliente).</summary>
    Task ClearSessionAsync(string sessionId);
}
