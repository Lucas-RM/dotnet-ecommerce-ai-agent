using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.Tools.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.LLM.Google;

/// <summary>
/// Monta o <see cref="Microsoft.SemanticKernel.Kernel"/> com Google AI (Gemini) e os plugins de e-commerce.
/// Mesmo padrão da <see cref="OpenAI.KernelFactory"/>, porém usando
/// <c>AddGoogleAIGeminiChatCompletion</c> e resolvendo plugins/filtros via <see cref="IServiceProvider"/>.
/// </summary>
public sealed class GoogleKernelFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public GoogleKernelFactory(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public Microsoft.SemanticKernel.Kernel CreateKernel(string sessionId)
    {
        var modelId = _configuration["LLM:Google:Model"]
            ?? throw new InvalidOperationException("Configuração ausente: LLM:Google:Model.");
        var apiKey = _configuration["LLM:Google:ApiKey"]
            ?? throw new InvalidOperationException("Configuração ausente: LLM:Google:ApiKey.");

        var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();

#pragma warning disable SKEXP0070
        builder.AddGoogleAIGeminiChatCompletion(modelId: modelId, apiKey: apiKey);
#pragma warning restore SKEXP0070

        builder.Services.AddSingleton<IFunctionInvocationFilter>(
            _serviceProvider.GetRequiredService<ApprovalFilter>());

        builder.Plugins.AddFromObject(
            _serviceProvider.GetRequiredService<ProductPlugin>(), nameof(ProductPlugin));
        builder.Plugins.AddFromObject(
            _serviceProvider.GetRequiredService<CartPlugin>(), nameof(CartPlugin));
        builder.Plugins.AddFromObject(
            _serviceProvider.GetRequiredService<OrderPlugin>(), nameof(OrderPlugin));

        var kernel = builder.Build();
        kernel.Data[AgentKernelDataKeys.SessionId] = sessionId;
        return kernel;
    }
}
