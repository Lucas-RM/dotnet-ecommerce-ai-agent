using ECommerce.AgentAPI.Application.Agents.PromptLayers;

namespace ECommerce.AgentAPI.Application.Agents;

/// <summary>
/// Prompt de sistema enviado ao LLM.
/// Composto por camadas para reduzir conflitos de edição em equipa.
/// </summary>
public static class AgentSystemPrompt
{
    public static readonly string DefaultText = AgentSystemPromptComposer.ComposeDefault();
}
