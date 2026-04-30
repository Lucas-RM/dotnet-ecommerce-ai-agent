using ECommerce.AgentAPI.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ECommerce.AgentAPI.Infrastructure.LLM;

public sealed class LLMProviderConfigurationValidationHostedService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILLMProviderResolver _providerResolver;
    private readonly IReadOnlyDictionary<LLMProvider, ILLMProviderConfigurationValidationStrategy> _validatorsByProvider;

    public LLMProviderConfigurationValidationHostedService(
        IConfiguration configuration,
        ILLMProviderResolver providerResolver,
        IEnumerable<ILLMProviderConfigurationValidationStrategy> validators)
    {
        _configuration = configuration;
        _providerResolver = providerResolver;
        _validatorsByProvider = validators.ToDictionary(v => v.Provider, v => v);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var provider = _providerResolver.Resolve();
        if (_validatorsByProvider.TryGetValue(provider, out var validator))
        {
            validator.Validate(_configuration);
            return Task.CompletedTask;
        }

        throw new NotSupportedException($"Provedor '{provider}' não suportado para validação de configuração.");
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        return Task.CompletedTask;
    }
}
