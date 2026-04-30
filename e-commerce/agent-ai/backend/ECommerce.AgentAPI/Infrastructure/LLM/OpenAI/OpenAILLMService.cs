using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.LLM;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ECommerce.AgentAPI.Infrastructure.LLM.OpenAI;

public sealed class OpenAILLMService : BaseLLMService
{
    private readonly OpenAIKernelFactory _kernelFactory;
    private readonly IServiceProvider _requestServices;

    public OpenAILLMService(
        OpenAIKernelFactory kernelFactory,
        ToolApprovalService toolApproval,
        IServiceProvider requestServices,
        ILogger<OpenAILLMService> logger)
        : base(toolApproval, logger)
    {
        _kernelFactory = kernelFactory;
        _requestServices = requestServices;
    }

    protected override Microsoft.SemanticKernel.Kernel CreateKernel(string sessionId)
    {
        return _kernelFactory.CreateKernel(sessionId, _requestServices);
    }

    protected override OpenAIPromptExecutionSettings CreatePromptExecutionSettings(LLMRequest request)
    {
        var settings = _kernelFactory.CreatePromptExecutionSettings();
        if (request.Temperature > 0)
        {
            settings.Temperature = request.Temperature;
        }

        if (request.MaxTokens > 0)
        {
            settings.MaxTokens = request.MaxTokens;
        }

        return settings;
    }

    protected override void LogChatCompletionError(ILogger logger, Exception ex)
    {
        logger.LogError(ex, "Falha no chat completion do OpenAI/SK.");
    }

    protected override string GetEmptyResponseWarningMessage() =>
        "A API OpenAI devolveu resposta vazia após a invocação.";
}
