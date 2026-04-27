namespace ECommerce.AgentAPI.Infrastructure.Observability;

internal static class AgentActivityNames
{
    public const string Source = "ECommerce.Agent";

    public const string ChatRequest = "agent.chat.request";

    public const string LlmGenerate = "agent.llm.generate";

    public const string ToolExecute = "agent.tool.execute";
}
