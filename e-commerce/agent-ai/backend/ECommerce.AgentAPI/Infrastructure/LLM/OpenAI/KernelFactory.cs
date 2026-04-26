using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Approval;
using Microsoft.Extensions.Configuration;
using ECommerce.AgentAPI.Infrastructure.Tools.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ECommerce.AgentAPI.Infrastructure.LLM.OpenAI;

/// <summary>
/// Monta o <see cref="Microsoft.SemanticKernel.Kernel"/> com OpenAI (api.openai.com) e os plugins de e-commerce.
/// </summary>
public sealed class KernelFactory : IKernelFactory
{
    private readonly IConfiguration _configuration;
    private readonly ToolApprovalService _toolApproval;

    public KernelFactory(IConfiguration configuration, ToolApprovalService toolApproval)
    {
        _configuration = configuration;
        _toolApproval = toolApproval;
    }

    public Microsoft.SemanticKernel.Kernel CreateKernel(IECommerceApi ecommerceApi, string sessionId)
    {
        if (!Guid.TryParse(sessionId, out var parsedSessionId))
        {
            throw new ArgumentException("SessionId inválido.", nameof(sessionId));
        }

        return CreateKernel(ecommerceApi, parsedSessionId);
    }

    public Microsoft.SemanticKernel.Kernel CreateKernel(IECommerceApi ecommerceApi, Guid sessionId)
    {
        ArgumentNullException.ThrowIfNull(ecommerceApi);

        // Secção 6 (ecommerce-agent-evolution): LLM:OpenAI — mantém fallback a OpenAI:* (legado)
        var model = _configuration["LLM:OpenAI:Model"] ?? _configuration["OpenAI:Model"]
            ?? throw new InvalidOperationException("Configuração ausente: LLM:OpenAI:Model (ou legado OpenAI:Model).");
        var apiKey = _configuration["LLM:OpenAI:ApiKey"] ?? _configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("Configuração ausente: LLM:OpenAI:ApiKey (ou legado OpenAI:ApiKey).");

        var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(modelId: model, apiKey: apiKey);

        builder.Services.AddSingleton(_toolApproval);
        builder.Services.AddSingleton<IFunctionInvocationFilter, ApprovalFilter>();

        builder.Plugins.AddFromObject(new ProductPlugin(ecommerceApi), nameof(ProductPlugin));
        builder.Plugins.AddFromObject(new CartPlugin(ecommerceApi), nameof(CartPlugin));
        builder.Plugins.AddFromObject(new OrderPlugin(ecommerceApi), nameof(OrderPlugin));

        var kernel = builder.Build();
        kernel.Data[AgentKernelDataKeys.SessionId] = sessionId;
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
