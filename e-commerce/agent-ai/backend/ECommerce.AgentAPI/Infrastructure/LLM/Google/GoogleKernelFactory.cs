using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.Tools.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.LLM.Google;

/// <summary>
/// Monta o <see cref="Microsoft.SemanticKernel.Kernel"/> com Google AI (Gemini) e os plugins de e-commerce.
/// Mesmo padrão da <see cref="OpenAI.KernelFactory"/>: recebe dependências scoped (ex.: <see cref="IECommerceApi"/>)
/// por parâmetro e instancia filtros/plugins manualmente, evitando resolver serviços scoped a partir do root provider
/// (o que dispararia <c>InvalidOperationException</c> "Cannot resolve scoped service ... from root provider.").
/// </summary>
public sealed class GoogleKernelFactory : IKernelFactory
{
    private readonly IConfiguration _configuration;
    private readonly ToolApprovalService _toolApproval;

    public GoogleKernelFactory(IConfiguration configuration, ToolApprovalService toolApproval)
    {
        _configuration = configuration;
        _toolApproval = toolApproval;
    }

    public Microsoft.SemanticKernel.Kernel CreateKernel(IECommerceApi ecommerceApi, string sessionId)
    {
        ArgumentNullException.ThrowIfNull(ecommerceApi);

        var modelId = _configuration["LLM:Google:Model"]
            ?? throw new InvalidOperationException("Configuração ausente: LLM:Google:Model.");
        var apiKey = _configuration["LLM:Google:ApiKey"]
            ?? throw new InvalidOperationException("Configuração ausente: LLM:Google:ApiKey.");

        var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();

#pragma warning disable SKEXP0070
        builder.AddGoogleAIGeminiChatCompletion(modelId: modelId, apiKey: apiKey);
#pragma warning restore SKEXP0070

        builder.Services.AddSingleton(_toolApproval);
        builder.Services.AddSingleton<IFunctionInvocationFilter, ApprovalFilter>();

        builder.Plugins.AddFromObject(new ProductPlugin(ecommerceApi), nameof(ProductPlugin));
        builder.Plugins.AddFromObject(new CartPlugin(ecommerceApi), nameof(CartPlugin));
        builder.Plugins.AddFromObject(new OrderPlugin(ecommerceApi), nameof(OrderPlugin));

        var kernel = builder.Build();
        kernel.Data[AgentKernelDataKeys.SessionId] = sessionId;
        return kernel;
    }
}
