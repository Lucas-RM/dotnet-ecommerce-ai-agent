using ECommerce.AgentAPI.Application.Options;
using Microsoft.Extensions.Options;

namespace ECommerce.AgentAPI.API.Filters;

public sealed class ChatEndpointAvailabilityFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var options = context.HttpContext.RequestServices.GetRequiredService<IOptions<AgentHostingOptions>>().Value;
        if (options.ChatEndpointEnabled)
        {
            return await next(context).ConfigureAwait(false);
        }

        return Results.Json(
            new
            {
                error = "Serviço de chat indisponível de momento. Tente mais tarde.",
                code = "agent_chat_disabled"
            },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
}
