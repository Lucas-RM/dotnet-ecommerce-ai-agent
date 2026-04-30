using ECommerce.AgentAPI.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ECommerce.AgentAPI.Infrastructure.LLM;

public sealed class LLMProviderConfigurationValidationHostedService : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IReadOnlyDictionary<LLMProvider, ILLMProviderConfigurationValidationStrategy> _validatorsByProvider;

    public LLMProviderConfigurationValidationHostedService(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        IEnumerable<ILLMProviderConfigurationValidationStrategy> validators)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _validatorsByProvider = validators.ToDictionary(v => v.Provider, v => v);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        using var scope = _scopeFactory.CreateScope();
        var providerResolver = scope.ServiceProvider.GetRequiredService<ILLMProviderResolver>();
        var provider = providerResolver.Resolve();
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
