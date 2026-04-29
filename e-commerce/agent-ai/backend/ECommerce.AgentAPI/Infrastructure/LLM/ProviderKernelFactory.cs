using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Infrastructure.LLM.Google;
using ECommerce.AgentAPI.Infrastructure.LLM.OpenAI;
namespace ECommerce.AgentAPI.Infrastructure.LLM;

public sealed class ProviderKernelFactory : IKernelFactory
{
    private readonly ILLMProviderResolver _providerResolver;
    private readonly OpenAIKernelFactory _openAiKernelFactory;
    private readonly GoogleKernelFactory _googleKernelFactory;

    public ProviderKernelFactory(
        ILLMProviderResolver providerResolver,
        OpenAIKernelFactory openAiKernelFactory,
        GoogleKernelFactory googleKernelFactory)
    {
        _providerResolver = providerResolver;
        _openAiKernelFactory = openAiKernelFactory;
        _googleKernelFactory = googleKernelFactory;
    }

    public Microsoft.SemanticKernel.Kernel CreateKernel(
        string sessionId,
        IServiceProvider requestServices)
    {
        var provider = _providerResolver.Resolve();
        return provider switch
        {
            LLMProvider.OpenAI => _openAiKernelFactory.CreateKernel(sessionId, requestServices),
            LLMProvider.Google => _googleKernelFactory.CreateKernel(sessionId, requestServices),
            _ => throw new NotSupportedException($"Provedor '{provider}' não suportado.")
        };
    }
}
