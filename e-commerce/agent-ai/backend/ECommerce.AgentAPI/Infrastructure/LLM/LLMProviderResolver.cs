using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Application.Agents.Routing;
using Microsoft.Extensions.Configuration;

namespace ECommerce.AgentAPI.Infrastructure.LLM;

public sealed class LLMProviderResolver : ILLMProviderResolver
{
    private readonly IConfiguration _configuration;
    private readonly IAgentExecutionContext _agentExecutionContext;

    public LLMProviderResolver(
        IConfiguration configuration,
        IAgentExecutionContext agentExecutionContext)
    {
        _configuration = configuration;
        _agentExecutionContext = agentExecutionContext;
    }

    public LLMProvider Resolve()
    {
        var byAgent = _agentExecutionContext.CurrentProfile?.LlmProvider;
        if (byAgent is not null)
            return byAgent.Value;

        var providerRaw = _configuration["LLM:Provider"] ?? "OpenAI";
        if (!Enum.TryParse<LLMProvider>(providerRaw, ignoreCase: true, out var provider))
        {
            provider = LLMProvider.OpenAI;
        }

        return provider;
    }
}
