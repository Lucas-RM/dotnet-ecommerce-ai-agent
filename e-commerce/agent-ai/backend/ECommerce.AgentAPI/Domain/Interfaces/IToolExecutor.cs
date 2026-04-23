using ECommerce.AgentAPI.Domain.Entities;
using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Domain.Interfaces;

public interface IToolExecutor
{
    Task<ToolExecutionResult> ExecuteAsync(ToolCall toolCall, string jwtToken, CancellationToken cancellationToken = default);
}
