namespace ECommerce.AgentAPI.Application.Options;

public sealed class AgentOptions
{
    public const string SectionName = "Agent";

    public int MaxConversationTurns { get; set; } = 20;
}
