using ECommerce.AgentAPI.Domain.Enums;

namespace ECommerce.AgentAPI.Infrastructure.LLM;

public sealed class ProviderKernelFactory : IKernelFactory
{
    private readonly ILLMProviderResolver _providerResolver;
    private readonly IReadOnlyDictionary<LLMProvider, IKernelFactory> _kernelsByProvider;

    public ProviderKernelFactory(
        ILLMProviderResolver providerResolver,
        IEnumerable<IKernelFactoryProviderStrategy> strategies)
    {
        _providerResolver = providerResolver;
        _kernelsByProvider = strategies.ToDictionary(s => s.Provider, s => s.KernelFactory);
    }

    public Microsoft.SemanticKernel.Kernel CreateKernel(
        string sessionId,
        IServiceProvider requestServices)
    {
        var provider = _providerResolver.Resolve();
        if (_kernelsByProvider.TryGetValue(provider, out var kernelFactory))
        {
            return kernelFactory.CreateKernel(sessionId, requestServices);
        }

        throw new NotSupportedException($"Provedor '{provider}' não suportado.");
    }
}
