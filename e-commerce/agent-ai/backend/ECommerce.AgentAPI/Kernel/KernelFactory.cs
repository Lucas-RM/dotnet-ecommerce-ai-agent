using ECommerce.AgentAPI.Approval;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace ECommerce.AgentAPI.Kernel;

/// <summary>
/// Monta o <see cref="Microsoft.SemanticKernel.Kernel"/> com OpenAI (api.openai.com) e os plugins de e-commerce.
/// </summary>
public sealed class KernelFactory
{
    private readonly IConfiguration _configuration;
    private readonly ToolApprovalService _toolApproval;

    public KernelFactory(IConfiguration configuration, ToolApprovalService toolApproval)
    {
        _configuration = configuration;
        _toolApproval = toolApproval;
    }

    /// <param name="ecommerceApi">Cliente Refit com JWT do request atual (via <c>ECommerceApiAuthorizationHandler</c>).</param>
    /// <param name="sessionId">Sessão do chat; propagada ao <see cref="ApprovalFilter"/> via <see cref="AgentKernelDataKeys.SessionId"/>.</param>
    public Microsoft.SemanticKernel.Kernel CreateKernel(IECommerceApi ecommerceApi, Guid sessionId)
    {
        ArgumentNullException.ThrowIfNull(ecommerceApi);

        var model = _configuration["OpenAI:Model"]
            ?? throw new InvalidOperationException("Configuração ausente: OpenAI:Model.");
        var apiKey = _configuration["OpenAI:ApiKey"]
            ?? throw new InvalidOperationException("Configuração ausente: OpenAI:ApiKey.");

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

    public OpenAIPromptExecutionSettings CreatePromptExecutionSettings()
    {
        return new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.3,
            MaxTokens = 1024
        };
    }
}
