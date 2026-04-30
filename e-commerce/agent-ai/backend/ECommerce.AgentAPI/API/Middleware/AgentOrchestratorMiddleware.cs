using System.Globalization;
using ECommerce.AgentAPI.Application.Abstractions;
using ECommerce.AgentAPI.Application.Agents.Routing;
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
    private readonly IAgentObservability _observability;
    private readonly IAgentRouter _agentRouter;
    private readonly IAgentExecutionContext _agentContext;

    public AgentOrchestratorMiddleware(
        ProcessUserMessageUseCase useCase,
        IHttpContextAccessor httpContext,
        IConfiguration configuration,
        ILogger<AgentOrchestratorMiddleware> logger,
        IAgentObservability observability,
        IAgentRouter agentRouter,
        IAgentExecutionContext agentContext)
    {
        _useCase = useCase;
        _httpContext = httpContext;
        _configuration = configuration;
        _logger = logger;
        _observability = observability;
        _agentRouter = agentRouter;
        _agentContext = agentContext;
    }

    public async Task<IResult> InvokeAsync(
        ChatRequest request,
        string? routeAgentId = null,
        CancellationToken cancellationToken = default)
    {
        _ = BearerTokenProvider.TryGetFromRequest(_httpContext.HttpContext?.Request, out var jwt);
        var token = jwt ?? string.Empty;
        var correlationId = ResolveCorrelationId(request);
        var profile = _agentRouter.Resolve(routeAgentId ?? request.AgentId);
        _agentContext.CurrentProfile = profile;

        var command = new ProcessMessageCommand
        {
            AgentId = profile.Id,
            SessionId = request.SessionId.ToString("D", CultureInfo.InvariantCulture),
            Message = request.Message,
            ApprovalId = request.ApprovalId,
            JwtToken = token,
            ClientVersion = request.ClientVersion,
            Locale = request.Locale,
            Channel = request.Channel,
            Metadata = request.Metadata,
            CorrelationId = correlationId
        };

        using (var _ = _observability.StartChatRequestActivity(command))
        {
            try
            {
                var result = await _useCase
                    .ExecuteAsync(command, cancellationToken)
                    .ConfigureAwait(false);
                EnrichLlmProvider(result.Response, profile.Id);
                EnrichCorrelation(result.Response, correlationId);
                return Results.Json(result.Response, statusCode: result.StatusCode);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogDebug(ex, "Operação de chat interrompida (cancelamento/timeout).");
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }
    }

    private string ResolveCorrelationId(ChatRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.CorrelationId))
            return request.CorrelationId!;

        return _httpContext.HttpContext?.TraceIdentifier
               ?? Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);
    }

    private void EnrichLlmProvider(ChatResponse? response, string agentId)
    {
        if (response is null)
        {
            return;
        }

        var raw = (_agentContext.CurrentProfile?.LlmProvider.ToString() ?? _configuration["LLM:Provider"] ?? "OpenAI").Trim();
        response.LlmProvider = string.Equals(raw, "Google", StringComparison.OrdinalIgnoreCase)
            || string.Equals(raw, "Gemini", StringComparison.OrdinalIgnoreCase)
            ? "google"
            : "openai";
        response.AgentId = agentId;
    }

    private static void EnrichCorrelation(ChatResponse? response, string correlationId)
    {
        if (response is null)
            return;

        response.CorrelationId = correlationId;
    }
}
