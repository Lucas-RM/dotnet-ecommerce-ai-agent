using ECommerce.AgentAPI.Application.Agents.Profiles;

namespace ECommerce.AgentAPI.Application.Agents.Prompting;

public interface IPromptComposer
{
    string ComposeSystemPrompt(IAgentProfile profile);
}
