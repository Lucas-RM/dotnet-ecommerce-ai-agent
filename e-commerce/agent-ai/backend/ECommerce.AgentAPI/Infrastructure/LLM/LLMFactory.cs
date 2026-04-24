using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.Infrastructure.LLM.Google;
using ECommerce.AgentAPI.Infrastructure.LLM.OpenAI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.AgentAPI.Infrastructure.LLM;

public sealed class LLMFactory : ILLMFactory
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _config;

    public LLMFactory(IServiceProvider sp, IConfiguration config)
    {
        _sp = sp;
        _config = config;
    }

    public ILLMService Create(LLMProvider provider) =>
        provider switch
        {
            LLMProvider.OpenAI => _sp.GetRequiredService<OpenAILLMService>(),
            LLMProvider.Google => _sp.GetRequiredService<GoogleLLMService>(),
            _ => throw new NotSupportedException($"Provedor '{provider}' não suportado.")
        };

    public ILLMService CreateFromConfig()
    {
        var s = _config["LLM:Provider"] ?? "OpenAI";
        if (!Enum.TryParse<LLMProvider>(s, ignoreCase: true, out var p))
        {
            p = LLMProvider.OpenAI;
        }

        return Create(p);
    }
}
