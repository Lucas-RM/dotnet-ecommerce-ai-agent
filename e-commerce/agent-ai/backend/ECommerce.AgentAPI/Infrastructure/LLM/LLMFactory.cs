using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Domain.Interfaces;
namespace ECommerce.AgentAPI.Infrastructure.LLM;

public sealed class LLMFactory : ILLMFactory
{
    private readonly ILLMProviderResolver _providerResolver;
    private readonly IReadOnlyDictionary<LLMProvider, ILLMService> _servicesByProvider;

    public LLMFactory(
        IEnumerable<ILLMServiceProviderStrategy> strategies,
        ILLMProviderResolver providerResolver)
    {
        _providerResolver = providerResolver;
        _servicesByProvider = strategies.ToDictionary(s => s.Provider, s => s.Service);
    }

    public ILLMService Create(LLMProvider provider)
    {
        if (_servicesByProvider.TryGetValue(provider, out var service))
        {
            return service;
        }

        throw new NotSupportedException($"Provedor '{provider}' não suportado.");
    }

    public ILLMService CreateFromConfig() => Create(_providerResolver.Resolve());
}
