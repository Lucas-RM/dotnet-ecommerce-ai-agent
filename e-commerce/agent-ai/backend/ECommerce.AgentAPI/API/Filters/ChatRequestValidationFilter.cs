using ECommerce.AgentAPI.Models;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.AgentAPI.API.Filters;

/// <summary>
/// Valida <see cref="ChatRequest"/> com FluentValidation antes do handler. Erros resultam
/// em <strong>400 Bad Request</strong> (RFC 7807 via <c>ValidationProblem</c>).
/// </summary>
public sealed class ChatRequestValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<ChatRequest>().FirstOrDefault();
        if (request is null)
            return await next(context).ConfigureAwait(false);

        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<ChatRequest>>();
        var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted).ConfigureAwait(false);
        if (!result.IsValid)
            return TypedResults.ValidationProblem(result.ToDictionary());

        return await next(context).ConfigureAwait(false);
    }
}
