using ECommerce.AgentAPI.Models;

namespace ECommerce.AgentAPI.Application.Abstractions;

/// <summary> Mapeia exceções de I/O, Refit, LLM e outras, para a resposta HTTP+chat do Agent. </summary>
public interface IChatErrorHandler
{
    /// <summary> Sempre retorna um resultado apropriado (não lança). </summary>
    ChatProcessResult MapToProcessResult(Exception exception);
}
