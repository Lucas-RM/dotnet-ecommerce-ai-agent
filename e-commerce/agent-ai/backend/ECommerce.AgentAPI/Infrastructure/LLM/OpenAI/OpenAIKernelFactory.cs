using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.LLM.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ECommerce.AgentAPI.Infrastructure.LLM.OpenAI;

/// <summary>
/// Monta o <see cref="Microsoft.SemanticKernel.Kernel"/> com OpenAI (api.openai.com) e os plugins de e-commerce.
/// O nome alinha com a fábrica do Google (mesmo padrão por fornecedor).
/// </summary>
public sealed class OpenAIKernelFactory : IKernelFactory
{
    private readonly IConfiguration _configuration;
    private readonly ToolApprovalService _toolApproval;
    private readonly IPluginFactory _pluginFactory;

    public OpenAIKernelFactory(
        IConfiguration configuration,
        ToolApprovalService toolApproval,
        IPluginFactory pluginFactory)
    {
        _configuration = configuration;
        _toolApproval = toolApproval;
        _pluginFactory = pluginFactory;
    }

    public Microsoft.SemanticKernel.Kernel CreateKernel(
        string sessionId,
        IServiceProvider requestServices)
    {
        if (!Guid.TryParse(sessionId, out var parsedSessionId))
        {
            throw new ArgumentException("SessionId inválido.", nameof(sessionId));
        }

        return CreateKernel(parsedSessionId, requestServices);
    }

    public Microsoft.SemanticKernel.Kernel CreateKernel(
        Guid sessionId,
        IServiceProvider requestServices)
    {
        ArgumentNullException.ThrowIfNull(requestServices);

        // Secção 6 (ecommerce-agent-evolution): LLM:OpenAI — mantém fallback a OpenAI:* (legado)
        var model = _configuration["LLM:OpenAI:Model"] ?? _configuration["OpenAI:Model"]
            ?? throw new InvalidOperationException("Configuração ausente: LLM:OpenAI:Model (ou legado OpenAI:Model).");
        var apiKey = _configuration["LLM:OpenAI:ApiKey"] ?? _configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("Configuração ausente: LLM:OpenAI:ApiKey (ou legado OpenAI:ApiKey).");

        var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(modelId: model, apiKey: apiKey);

        builder.Services.AddSingleton(_toolApproval);
        builder.Services.AddSingleton<IFunctionInvocationFilter, ApprovalFilter>();

        builder.AddRegisteredToolPlugins(_pluginFactory, requestServices);

        var kernel = builder.Build();
        kernel.Data[AgentKernelDataKeys.SessionId] = sessionId;
        kernel.Data[AgentKernelDataKeys.AutomaticToolInvocations] = new List<RecordedToolInvocation>();
        return kernel;
    }

    public OpenAIPromptExecutionSettings CreatePromptExecutionSettings() =>
        new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.3,
            MaxTokens = 1024
        };
}
