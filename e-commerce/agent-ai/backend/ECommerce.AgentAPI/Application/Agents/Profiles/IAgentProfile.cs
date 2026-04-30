using ECommerce.AgentAPI.Domain.Enums;

namespace ECommerce.AgentAPI.Application.Agents.Profiles;

public interface IAgentProfile
{
    string Id { get; }
    string DisplayName { get; }
    string Description { get; }
    LLMProvider LlmProvider { get; }
    string Model { get; }
    string PromptTemplate { get; }
    string ApprovalPolicy { get; }
    IReadOnlyCollection<string> EnabledPlugins { get; }
    IReadOnlyCollection<string> EnabledTools { get; }
}
