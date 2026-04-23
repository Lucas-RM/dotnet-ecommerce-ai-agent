using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Domain.Interfaces;

public interface ILLMService
{
    Task<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default);
}
