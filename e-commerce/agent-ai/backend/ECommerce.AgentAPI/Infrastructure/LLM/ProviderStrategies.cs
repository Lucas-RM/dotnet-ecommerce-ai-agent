using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.Infrastructure.LLM.Google;
using ECommerce.AgentAPI.Infrastructure.LLM.OpenAI;
using Microsoft.Extensions.Configuration;

namespace ECommerce.AgentAPI.Infrastructure.LLM;

public interface ILLMServiceProviderStrategy
{
    LLMProvider Provider { get; }

    ILLMService Service { get; }
}

public interface IKernelFactoryProviderStrategy
{
    LLMProvider Provider { get; }

    IKernelFactory KernelFactory { get; }
}

public interface ILLMProviderConfigurationValidationStrategy
{
    LLMProvider Provider { get; }

    void Validate(IConfiguration configuration);
}

public sealed class OpenAILLMServiceProviderStrategy : ILLMServiceProviderStrategy
{
    public OpenAILLMServiceProviderStrategy(OpenAILLMService service)
    {
        Service = service;
    }

    public LLMProvider Provider => LLMProvider.OpenAI;

    public ILLMService Service { get; }
}

public sealed class GoogleLLMServiceProviderStrategy : ILLMServiceProviderStrategy
{
    public GoogleLLMServiceProviderStrategy(GoogleLLMService service)
    {
        Service = service;
    }

    public LLMProvider Provider => LLMProvider.Google;

    public ILLMService Service { get; }
}

public sealed class OpenAIKernelFactoryProviderStrategy : IKernelFactoryProviderStrategy
{
    public OpenAIKernelFactoryProviderStrategy(OpenAIKernelFactory kernelFactory)
    {
        KernelFactory = kernelFactory;
    }

    public LLMProvider Provider => LLMProvider.OpenAI;

    public IKernelFactory KernelFactory { get; }
}

public sealed class GoogleKernelFactoryProviderStrategy : IKernelFactoryProviderStrategy
{
    public GoogleKernelFactoryProviderStrategy(GoogleKernelFactory kernelFactory)
    {
        KernelFactory = kernelFactory;
    }

    public LLMProvider Provider => LLMProvider.Google;

    public IKernelFactory KernelFactory { get; }
}

public sealed class OpenAILLMProviderConfigurationValidationStrategy : ILLMProviderConfigurationValidationStrategy
{
    public LLMProvider Provider => LLMProvider.OpenAI;

    public void Validate(IConfiguration configuration)
    {
        var apiKey = configuration["LLM:OpenAI:ApiKey"] ?? configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Configuração ausente: LLM:OpenAI:ApiKey (ou legado OpenAI:ApiKey) é obrigatória quando LLM:Provider=OpenAI.");
        }
    }
}

public sealed class GoogleLLMProviderConfigurationValidationStrategy : ILLMProviderConfigurationValidationStrategy
{
    public LLMProvider Provider => LLMProvider.Google;

    public void Validate(IConfiguration configuration)
    {
        var apiKey = configuration["LLM:Google:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Configuração ausente: LLM:Google:ApiKey é obrigatória quando LLM:Provider=Google.");
        }
    }
}
