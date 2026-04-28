namespace ECommerce.AgentAPI.Application.Agents.PromptLayers;

internal static class AgentSystemPromptComposer
{
    public static string ComposeDefault()
    {
        return Compose(
            PersonaPromptLayer.Text,
            ToolPolicyPromptLayer.Text,
            CompliancePromptLayer.Text,
            TonePromptLayer.Text);
    }

    public static string Compose(
        string persona,
        string toolPolicy,
        string compliance,
        string tone)
    {
        return string.Join("\n\n", persona, toolPolicy, compliance, tone);
    }
}
