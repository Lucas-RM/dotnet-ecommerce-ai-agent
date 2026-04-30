using ECommerce.AgentAPI.Domain.Enums;

namespace ECommerce.AgentAPI.Configuration.Options;

public sealed class AgentCatalogOptions
{
    public const string SectionName = "Agents";

    public string DefaultAgentId { get; set; } = "support-general";

    public List<AgentCatalogItemOptions> Catalog { get; set; } = [];
}

public sealed class AgentCatalogItemOptions
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public LLMProvider LlmProvider { get; set; } = LLMProvider.OpenAI;

    public string Model { get; set; } = string.Empty;

    public string PromptTemplate { get; set; } = string.Empty;

    public List<string> EnabledPlugins { get; set; } = [];

    public List<string> EnabledTools { get; set; } = [];

    public string ApprovalPolicy { get; set; } = "ByCapability";
}
