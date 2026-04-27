namespace ECommerce.AgentAPI.Infrastructure.Approval;

public interface IToolApprovalArgumentEnrichmentStrategy
{
    bool CanHandle(string toolName);

    Task<ApprovalArgumentEnrichment> EnrichAsync(
        string toolName,
        Dictionary<string, object> arguments,
        CancellationToken cancellationToken = default);
}
