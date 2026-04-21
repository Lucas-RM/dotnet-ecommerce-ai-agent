namespace ECommerce.AgentAPI.Models;

/// <summary>Resultado do processamento do chat: status HTTP + corpo <see cref="ChatResponse"/>.</summary>
public sealed record ChatProcessResult(int StatusCode, ChatResponse Response);
