using System.Diagnostics;
using ECommerce.AgentAPI.Application.Capabilities;
using ECommerce.AgentAPI.Application.DTOs;

namespace ECommerce.AgentAPI.Application.Abstractions;

public interface IAgentObservability
{
    Activity? StartChatRequestActivity(ProcessMessageCommand command);

    Activity? StartLlmActivity(string sessionId, string? correlationId);

    Activity? StartToolActivity(string toolName, string sessionId, string? correlationId);

    void RecordLlmDuration(TimeSpan duration, string outcome, string? toolName, AgentCapability capability);

    void RecordToolDuration(
        string toolName,
        AgentCapability capability,
        TimeSpan duration,
        bool success,
        string? errorKind);

    void RecordApproval(string eventName, string? toolName, AgentCapability capability);

    void RecordEnvelopeInvalid(string toolName, string? dataType, string? reason);

    void AddChatSummaryTags(string sessionId, string? correlationId, string outcome);
}
