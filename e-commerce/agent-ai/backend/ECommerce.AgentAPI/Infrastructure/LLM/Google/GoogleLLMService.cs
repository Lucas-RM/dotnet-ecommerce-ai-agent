using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.LLM;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using Microsoft.Extensions.Logging;

namespace ECommerce.AgentAPI.Infrastructure.LLM.Google;

/// <summary>
/// Implementação de <see cref="ILLMService"/> para Google AI (Gemini), seguindo o mesmo
/// fluxo do <see cref="OpenAI.OpenAILLMService"/>: monta <see cref="ChatHistory"/>,
/// invoca o chat completion via Semantic Kernel e extrai a <see cref="LLMResponse"/>.
/// </summary>
public sealed class GoogleLLMService : BaseLLMService
{
    private const double DefaultTemperature = 0.3;
    private const int DefaultMaxOutputTokens = 1024;

    private readonly GoogleKernelFactory _kernelFactory;
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _requestServices;

    public GoogleLLMService(
        GoogleKernelFactory kernelFactory,
        ToolApprovalService toolApproval,
        IConfiguration configuration,
        IServiceProvider requestServices,
        ILogger<GoogleLLMService> logger)
        : base(toolApproval, logger)
    {
        _kernelFactory = kernelFactory;
        _configuration = configuration;
        _requestServices = requestServices;
    }

    protected override Microsoft.SemanticKernel.Kernel CreateKernel(string sessionId)
    {
        return _kernelFactory.CreateKernel(sessionId, _requestServices);
    }

#pragma warning disable SKEXP0070
    protected override GeminiPromptExecutionSettings CreatePromptExecutionSettings(LLMRequest request)
    {
        var temperature = request.Temperature > 0
            ? request.Temperature
            : GetConfiguredTemperature();

        var maxTokens = request.MaxTokens > 0
            ? request.MaxTokens
            : GetConfiguredMaxOutputTokens();

        return new GeminiPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = temperature,
            MaxTokens = maxTokens
        };
    }
#pragma warning restore SKEXP0070

    protected override void LogChatCompletionError(ILogger logger, Exception ex)
    {
        logger.LogError(ex, "Falha no chat completion do Google/Gemini/SK.");
    }

    protected override string GetEmptyResponseWarningMessage() =>
        "A API Google/Gemini devolveu resposta vazia após a invocação.";

    private double GetConfiguredTemperature()
    {
        if (double.TryParse(
                _configuration["LLM:Google:Temperature"],
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var t) && t >= 0)
        {
            return t;
        }
        return DefaultTemperature;
    }

    private int GetConfiguredMaxOutputTokens()
    {
        if (int.TryParse(_configuration["LLM:Google:MaxOutputTokens"], out var n) && n > 0)
        {
            return n;
        }
        return DefaultMaxOutputTokens;
    }

}
