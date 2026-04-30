using ECommerce.AgentAPI.Configuration.Options;
using ECommerce.AgentAPI.Domain.Enums;

namespace ECommerce.AgentAPI.Application.Agents.Profiles;

public sealed class ConfiguredAgentProfile : IAgentProfile
{
    public ConfiguredAgentProfile(AgentCatalogItemOptions item)
    {
        Id = item.Id;
        DisplayName = item.DisplayName;
        Description = item.Description;
        LlmProvider = item.LlmProvider;
        Model = item.Model;
        PromptTemplate = item.PromptTemplate;
        ApprovalPolicy = item.ApprovalPolicy;
        EnabledPlugins = item.EnabledPlugins;
        EnabledTools = item.EnabledTools;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string Description { get; }
    public LLMProvider LlmProvider { get; }
    public string Model { get; }
    public string PromptTemplate { get; }
    public string ApprovalPolicy { get; }
    public IReadOnlyCollection<string> EnabledPlugins { get; }
    public IReadOnlyCollection<string> EnabledTools { get; }
}
