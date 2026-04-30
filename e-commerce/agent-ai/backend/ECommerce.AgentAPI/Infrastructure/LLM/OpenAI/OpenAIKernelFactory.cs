using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.LLM;
using ECommerce.AgentAPI.Infrastructure.LLM.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ECommerce.AgentAPI.Infrastructure.LLM.OpenAI;

/// <summary>
/// Monta o <see cref="Microsoft.SemanticKernel.Kernel"/> com OpenAI (api.openai.com) e os plugins de e-commerce.
/// O nome alinha com a fábrica do Google (mesmo padrão por fornecedor).
/// </summary>
public sealed class OpenAIKernelFactory : BaseProviderKernelFactory
{
    public OpenAIKernelFactory(
        IConfiguration configuration,
        ToolApprovalService toolApproval,
        IPluginFactory pluginFactory)
        : base(configuration, toolApproval, pluginFactory)
    {
    }

    protected override void ConfigureProviderChatCompletion(IKernelBuilder builder)
    {
        // Secção 6 (ecommerce-agent-evolution): LLM:OpenAI — mantém fallback a OpenAI:* (legado)
        var model = Configuration["LLM:OpenAI:Model"] ?? Configuration["OpenAI:Model"]
            ?? throw new InvalidOperationException("Configuração ausente: LLM:OpenAI:Model (ou legado OpenAI:Model).");
        var apiKey = Configuration["LLM:OpenAI:ApiKey"] ?? Configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("Configuração ausente: LLM:OpenAI:ApiKey (ou legado OpenAI:ApiKey).");

        builder.AddOpenAIChatCompletion(modelId: model, apiKey: apiKey);
    }

    public OpenAIPromptExecutionSettings CreatePromptExecutionSettings() =>
        new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.3,
            MaxTokens = 1024
        };
}
