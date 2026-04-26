using ECommerce.AgentAPI.Domain.Enums;

namespace ECommerce.AgentAPI.Infrastructure.LLM;

public interface ILLMProviderResolver
{
    LLMProvider Resolve();
}
