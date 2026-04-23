using ECommerce.AgentAPI.Domain.Entities;
using ECommerce.AgentAPI.Domain.Enums;

namespace ECommerce.AgentAPI.Domain.Interfaces;

public interface IToolApprovalService
{
    bool RequiresApproval(string toolName);

    Task StorePendingAsync(PendingApproval pending);

    Task<PendingApproval?> GetPendingAsync(string sessionId);

    Task ClearPendingAsync(string sessionId);

    ApprovalClassification ClassifyUserResponse(string message);
}
