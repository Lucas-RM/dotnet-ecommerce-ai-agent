using ECommerce.AgentAPI.Domain.Enums;

namespace ECommerce.AgentAPI.Domain.Interfaces;

public interface ILLMFactory
{
    ILLMService Create(LLMProvider provider);

    ILLMService CreateFromConfig();
}
