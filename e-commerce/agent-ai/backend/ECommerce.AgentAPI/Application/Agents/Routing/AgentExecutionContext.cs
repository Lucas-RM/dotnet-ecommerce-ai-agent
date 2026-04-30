using ECommerce.AgentAPI.Application.Agents.Profiles;

namespace ECommerce.AgentAPI.Application.Agents.Routing;

public interface IAgentExecutionContext
{
    IAgentProfile? CurrentProfile { get; set; }
}

public sealed class AgentExecutionContext : IAgentExecutionContext
{
    public IAgentProfile? CurrentProfile { get; set; }
}
