using ECommerce.Application.Common;
using ECommerce.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace ECommerce.API.Middlewares;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Falha de validação FluentValidation");
            var errors = ex.Errors.Select(e => string.IsNullOrEmpty(e.PropertyName) ? e.ErrorMessage : $"{e.PropertyName}: {e.ErrorMessage}");
            await WriteJsonAsync(context, (int)HttpStatusCode.BadRequest, ApiResponse<object>.Fail("Erro de validação.", errors));
        }
        catch (NotFoundDomainException ex)
        {
            logger.LogWarning(ex, "Não encontrado: {Message}", ex.Message);
            await WriteJsonAsync(context, (int)HttpStatusCode.NotFound, ApiResponse<string>.Fail(ex.Message));
        }
        catch (ConflictDomainException ex)
        {
            logger.LogWarning(ex, "Conflito: {Message}", ex.Message);
            await WriteJsonAsync(context, (int)HttpStatusCode.Conflict, ApiResponse<string>.Fail(ex.Message));
        }
        catch (UnauthorizedDomainException ex)
        {
            logger.LogWarning(ex, "Não autorizado (regra): {Message}", ex.Message);
            await WriteJsonAsync(context, (int)HttpStatusCode.Unauthorized, ApiResponse<string>.Fail(ex.Message));
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Regra de negócio: {Message}", ex.Message);
            await WriteJsonAsync(context, (int)HttpStatusCode.BadRequest, ApiResponse<string>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado");
            var message = env.IsDevelopment() ? ex.Message : "Ocorreu um erro interno.";
            await WriteJsonAsync(context, (int)HttpStatusCode.InternalServerError, ApiResponse<string>.Fail(message));
        }
    }

    private static async Task WriteJsonAsync<T>(HttpContext context, int statusCode, ApiResponse<T> body)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
