using ECommerce.AgentAPI.Models;
using FluentValidation;

namespace ECommerce.AgentAPI.Endpoints;

/// <summary>Valida <see cref="ChatRequest"/> com FluentValidation em Minimal APIs.</summary>
internal sealed class ChatRequestValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<ChatRequest>().FirstOrDefault();
        if (request is null)
            return await next(context).ConfigureAwait(false);

        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<ChatRequest>>();
        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted).ConfigureAwait(false);
        if (!result.IsValid)
            return Results.ValidationProblem(result.ToDictionary());

        return await next(context).ConfigureAwait(false);
    }
}
