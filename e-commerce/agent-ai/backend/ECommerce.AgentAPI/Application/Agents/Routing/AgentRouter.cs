using ECommerce.AgentAPI.Application.Agents.Profiles;
using ECommerce.AgentAPI.Configuration.Options;
using Microsoft.Extensions.Options;

namespace ECommerce.AgentAPI.Application.Agents.Routing;

public sealed class AgentRouter : IAgentRouter
{
    private readonly AgentCatalogOptions _options;
    private readonly IReadOnlyDictionary<string, IAgentProfile> _profiles;

    public AgentRouter(IOptions<AgentCatalogOptions> options)
    {
        _options = options.Value;
        _profiles = _options.Catalog
            .Select(x => new ConfiguredAgentProfile(x))
            .ToDictionary(x => x.Id, x => (IAgentProfile)x, StringComparer.OrdinalIgnoreCase);
    }

    public IAgentProfile Resolve(string? agentId)
    {
        var canonicalId = string.IsNullOrWhiteSpace(agentId)
            ? _options.DefaultAgentId
            : agentId.Trim();

        if (_profiles.TryGetValue(canonicalId, out var profile))
            return profile;

        if (_profiles.TryGetValue(_options.DefaultAgentId, out var fallback))
            return fallback;

        throw new InvalidOperationException("Nenhum perfil de agente configurado.");
    }

    public IReadOnlyCollection<IAgentProfile> GetAvailable() => _profiles.Values.ToArray();
}
