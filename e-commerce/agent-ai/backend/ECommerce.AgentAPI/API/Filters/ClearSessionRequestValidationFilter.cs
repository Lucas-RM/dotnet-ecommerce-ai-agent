using ECommerce.AgentAPI.Models;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.AgentAPI.API.Filters;

public sealed class ClearSessionRequestValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<ClearSessionRequest>().FirstOrDefault();
        if (request is null)
            return await next(context).ConfigureAwait(false);

        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<ClearSessionRequest>>();
        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted).ConfigureAwait(false);
        if (!result.IsValid)
            return TypedResults.ValidationProblem(result.ToDictionary());

        return await next(context).ConfigureAwait(false);
    }
}
