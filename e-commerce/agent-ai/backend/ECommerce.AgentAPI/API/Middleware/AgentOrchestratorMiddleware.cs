using System.Globalization;
using ECommerce.AgentAPI.Application.DTOs;
using ECommerce.AgentAPI.Application.UseCases;
using ECommerce.AgentAPI.Models;
using ECommerce.AgentAPI.API.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.AgentAPI.API.Middleware;

/// <summary>
/// Camada de entrada do Agent: obtém o JWT, delega a <see cref="ProcessUserMessageUseCase" />
/// e mapeia o resultado a <see cref="IResult" />. (Orquestração de negócio fica no Application.)
/// </summary>
public sealed class AgentOrchestratorMiddleware
{
    private readonly ProcessUserMessageUseCase _useCase;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AgentOrchestratorMiddleware> _logger;

    public AgentOrchestratorMiddleware(
        ProcessUserMessageUseCase useCase,
        IHttpContextAccessor httpContext,
        IConfiguration configuration,
        ILogger<AgentOrchestratorMiddleware> logger)
    {
        _useCase = useCase;
        _httpContext = httpContext;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IResult> InvokeAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        _ = BearerTokenProvider.TryGetFromRequest(_httpContext.HttpContext?.Request, out var jwt);
        var token = jwt ?? string.Empty;

        var command = new ProcessMessageCommand
        {
            SessionId = request.SessionId.ToString("D", CultureInfo.InvariantCulture),
            Message = request.Message,
            JwtToken = token
        };

        try
        {
            var result = await _useCase
                .ExecuteAsync(command, cancellationToken)
                .ConfigureAwait(false);
            EnrichLlmProvider(result.Response);
            return Results.Json(result.Response, statusCode: result.StatusCode);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogDebug(ex, "Operação de chat interrompida (cancelamento/timeout).");
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private void EnrichLlmProvider(ChatResponse? response)
    {
        if (response is null)
        {
            return;
        }

        var raw = (_configuration["LLM:Provider"] ?? "OpenAI").Trim();
        if (string.Equals(raw, "Google", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "Gemini", StringComparison.OrdinalIgnoreCase))
        {
            response.LlmProvider = "google";
        }
        else
        {
            response.LlmProvider = "openai";
        }
    }
}
