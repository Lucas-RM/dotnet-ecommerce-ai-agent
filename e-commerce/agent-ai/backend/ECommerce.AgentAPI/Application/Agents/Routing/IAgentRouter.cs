using ECommerce.AgentAPI.Application.Agents.Profiles;

namespace ECommerce.AgentAPI.Application.Agents.Routing;

public interface IAgentRouter
{
    IAgentProfile Resolve(string? agentId);
    IReadOnlyCollection<IAgentProfile> GetAvailable();
}
