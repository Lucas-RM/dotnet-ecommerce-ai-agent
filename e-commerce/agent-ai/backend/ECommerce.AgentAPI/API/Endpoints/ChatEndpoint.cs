using System.Globalization;
using ECommerce.AgentAPI.API.Filters;
using ECommerce.AgentAPI.Application.Agents.Routing;
using ECommerce.AgentAPI.API.Middleware;
using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Timeouts;

namespace ECommerce.AgentAPI.API.Endpoints;

public static class ChatEndpoint
{
    public static void Map(WebApplication app, string chatRateLimitPolicy)
    {
        var timeoutSec = app.Configuration.GetValue("Agent:Hosting:ChatRequestTimeoutSeconds", 120);
        var chatTimeout = TimeSpan.FromSeconds(timeoutSec);

        app.MapPost(
                "/api/agent/chat",
                (
                    ChatRequest request,
                    AgentOrchestratorMiddleware orchestrator,
                    CancellationToken cancellationToken) =>
                    orchestrator.InvokeAsync(request, null, cancellationToken))
            .AddEndpointFilter<ChatEndpointAvailabilityFilter>()
            .AddEndpointFilter<ChatRequestValidationFilter>()
            .WithRequestTimeout(chatTimeout)
            .RequireAuthorization()
            .RequireRateLimiting(chatRateLimitPolicy);

        app.MapPost(
                "/api/agents/{agentId}/chat",
                (
                    string agentId,
                    ChatRequest request,
                    AgentOrchestratorMiddleware orchestrator,
                    CancellationToken cancellationToken) =>
                    orchestrator.InvokeAsync(request, agentId, cancellationToken))
            .AddEndpointFilter<ChatEndpointAvailabilityFilter>()
            .AddEndpointFilter<ChatRequestValidationFilter>()
            .WithRequestTimeout(chatTimeout)
            .RequireAuthorization()
            .RequireRateLimiting(chatRateLimitPolicy);

        app.MapGet(
                "/api/agents",
                (IAgentRouter router) =>
                    Results.Ok(router.GetAvailable()
                        .Select(a => new
                        {
                            a.Id,
                            a.DisplayName,
                            a.Description,
                            provider = a.LlmProvider.ToString(),
                            model = a.Model,
                            enabledTools = a.EnabledTools
                        })))
            .RequireAuthorization();

        app.MapGet(
                "/api/agents/{agentId}",
                (string agentId, IAgentRouter router) =>
                {
                    var profile = router.Resolve(agentId);
                    return Results.Ok(new
                    {
                        profile.Id,
                        profile.DisplayName,
                        profile.Description,
                        provider = profile.LlmProvider.ToString(),
                        model = profile.Model,
                        promptTemplate = profile.PromptTemplate,
                        enabledPlugins = profile.EnabledPlugins,
                        enabledTools = profile.EnabledTools
                    });
                })
            .RequireAuthorization();

        app.MapPost(
                "/api/agents/{agentId}/chat/approvals/{approvalId}",
                (
                    string agentId,
                    string approvalId,
                    ApprovalDecisionRequest request,
                    AgentOrchestratorMiddleware orchestrator,
                    CancellationToken cancellationToken) =>
                {
                    _ = approvalId;
                    var chatRequest = new ChatRequest
                    {
                        AgentId = agentId,
                        SessionId = request.SessionId,
                        Message = request.Decision,
                        ApprovalId = approvalId,
                        CorrelationId = request.CorrelationId
                    };
                    return orchestrator.InvokeAsync(chatRequest, agentId, cancellationToken);
                })
            .AddEndpointFilter<ChatEndpointAvailabilityFilter>()
            .WithRequestTimeout(chatTimeout)
            .RequireAuthorization()
            .RequireRateLimiting(chatRateLimitPolicy);

        app.MapPost(
                "/api/agent/chat/session/clear",
                async (ClearSessionRequest request, IMemoryService memory, IToolApprovalService approval) =>
                {
                    var sessionId = request.SessionId.ToString("D", CultureInfo.InvariantCulture);
                    await memory.ClearSessionAsync(sessionId).ConfigureAwait(false);
                    await approval.ClearPendingBySessionIdAsync(sessionId).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .AddEndpointFilter<ChatEndpointAvailabilityFilter>()
            .AddEndpointFilter<ClearSessionRequestValidationFilter>()
            .RequireAuthorization()
            .RequireRateLimiting(chatRateLimitPolicy);
    }
}
