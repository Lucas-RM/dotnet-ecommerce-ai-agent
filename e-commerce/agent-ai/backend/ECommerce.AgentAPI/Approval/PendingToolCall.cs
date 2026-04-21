using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Approval;

/// <summary>Tool call bloqueada pelo <see cref="ApprovalFilter"/>, aguardando confirmação do usuário.</summary>
public sealed class PendingToolCall
{
    public required string PluginName { get; init; }
    public required string FunctionName { get; init; }
    public required KernelArguments Arguments { get; init; }
    public required string ApprovalMessage { get; init; }
    public DateTimeOffset StoredAt { get; init; } = DateTimeOffset.UtcNow;
}
