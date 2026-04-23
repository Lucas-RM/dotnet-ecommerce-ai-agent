using ECommerce.AgentAPI.API.Filters;
using ECommerce.AgentAPI.API.Middleware;
using ECommerce.AgentAPI.Models;
using Microsoft.AspNetCore.Builder;

namespace ECommerce.AgentAPI.API.Endpoints;

public static class ChatEndpoint
{
    public static void Map(WebApplication app, string chatRateLimitPolicy) =>
        app.MapPost(
                "/api/agent/chat",
                (
                    ChatRequest request,
                    AgentOrchestratorMiddleware orchestrator,
                    CancellationToken cancellationToken) =>
                    orchestrator.InvokeAsync(request, cancellationToken))
            .RequireAuthorization()
            .RequireRateLimiting(chatRateLimitPolicy)
            .AddEndpointFilter<ChatRequestValidationFilter>();
}
