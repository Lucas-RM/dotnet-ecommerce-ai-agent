using ECommerce.AgentAPI.Middleware;
using ECommerce.AgentAPI.Models;

namespace ECommerce.AgentAPI.Endpoints;

public static class ChatEndpoint
{
    public static void Map(WebApplication app, string chatRateLimitPolicy)
    {
        app.MapPost(
                "/api/agent/chat",
                async (
                    ChatRequest request,
                    AgentOrchestratorMiddleware orchestrator,
                    CancellationToken cancellationToken) =>
                {
                    var result = await orchestrator.ProcessAsync(request, cancellationToken).ConfigureAwait(false);
                    return Results.Json(result.Response, statusCode: result.StatusCode);
                })
            .AddEndpointFilter<ChatRequestValidationFilter>()
            .RequireRateLimiting(chatRateLimitPolicy)
            .RequireAuthorization();
    }
}
