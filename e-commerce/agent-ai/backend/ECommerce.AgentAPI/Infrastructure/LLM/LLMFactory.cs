using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.Infrastructure.LLM.Google;
using ECommerce.AgentAPI.Infrastructure.LLM.OpenAI;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.AgentAPI.Infrastructure.LLM;

public sealed class LLMFactory : ILLMFactory
{
    private readonly IServiceProvider _sp;
    private readonly ILLMProviderResolver _providerResolver;

    public LLMFactory(IServiceProvider sp, ILLMProviderResolver providerResolver)
    {
        _sp = sp;
        _providerResolver = providerResolver;
    }

    public ILLMService Create(LLMProvider provider) =>
        provider switch
        {
            LLMProvider.OpenAI => _sp.GetRequiredService<OpenAILLMService>(),
            LLMProvider.Google => _sp.GetRequiredService<GoogleLLMService>(),
            _ => throw new NotSupportedException($"Provedor '{provider}' não suportado.")
        };

    public ILLMService CreateFromConfig() => Create(_providerResolver.Resolve());
}
