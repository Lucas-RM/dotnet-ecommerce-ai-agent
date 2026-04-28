using ECommerce.AgentAPI.Application.Agents.PromptLayers;

namespace ECommerce.AgentAPI.Application.Options;

/// <summary>Prompt de sistema do agente, com fallback seguro no código.</summary>
public sealed class AgentPromptOptions
{
    public const string SectionName = "Agent:Prompt";

    /// <summary>
    /// Override completo do prompt final. Quando preenchido, ignora overrides por camada.
    /// </summary>
    public string SystemPrompt { get; set; } = string.Empty;

    public string Persona { get; set; } = string.Empty;

    public string ToolPolicy { get; set; } = string.Empty;

    public string Compliance { get; set; } = string.Empty;

    public string Tone { get; set; } = string.Empty;

    public string ResolveSystemPrompt()
    {
        if (!string.IsNullOrWhiteSpace(SystemPrompt))
        {
            return SystemPrompt;
        }

        var persona = string.IsNullOrWhiteSpace(Persona) ? PersonaPromptLayer.Text : Persona;
        var toolPolicy = string.IsNullOrWhiteSpace(ToolPolicy) ? ToolPolicyPromptLayer.Text : ToolPolicy;
        var compliance = string.IsNullOrWhiteSpace(Compliance) ? CompliancePromptLayer.Text : Compliance;
        var tone = string.IsNullOrWhiteSpace(Tone) ? TonePromptLayer.Text : Tone;

        return AgentSystemPromptComposer.Compose(persona, toolPolicy, compliance, tone);
    }
}
