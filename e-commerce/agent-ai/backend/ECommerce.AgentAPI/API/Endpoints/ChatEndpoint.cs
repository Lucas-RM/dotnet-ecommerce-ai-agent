using System.Globalization;
using ECommerce.AgentAPI.API.Filters;
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
                    orchestrator.InvokeAsync(request, cancellationToken))
            .AddEndpointFilter<ChatEndpointAvailabilityFilter>()
            .AddEndpointFilter<ChatRequestValidationFilter>()
            .WithRequestTimeout(chatTimeout)
            .RequireAuthorization()
            .RequireRateLimiting(chatRateLimitPolicy);

        app.MapPost(
                "/api/agent/chat/session/clear",
                async (ClearSessionRequest request, IMemoryService memory, IToolApprovalService approval) =>
                {
                    var sessionId = request.SessionId.ToString("D", CultureInfo.InvariantCulture);
                    await memory.ClearSessionAsync(sessionId).ConfigureAwait(false);
                    await approval.ClearPendingAsync(sessionId).ConfigureAwait(false);
                    return Results.NoContent();
                })
            .AddEndpointFilter<ChatEndpointAvailabilityFilter>()
            .AddEndpointFilter<ClearSessionRequestValidationFilter>()
            .RequireAuthorization()
            .RequireRateLimiting(chatRateLimitPolicy);
    }
}
