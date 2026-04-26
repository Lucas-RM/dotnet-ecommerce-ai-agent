using ECommerce.AgentAPI.Domain.Enums;
using Microsoft.Extensions.Configuration;

namespace ECommerce.AgentAPI.Infrastructure.LLM;

public sealed class LLMProviderResolver : ILLMProviderResolver
{
    private readonly IConfiguration _configuration;

    public LLMProviderResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public LLMProvider Resolve()
    {
        var providerRaw = _configuration["LLM:Provider"] ?? "OpenAI";
        if (!Enum.TryParse<LLMProvider>(providerRaw, ignoreCase: true, out var provider))
        {
            provider = LLMProvider.OpenAI;
        }

        return provider;
    }
}
