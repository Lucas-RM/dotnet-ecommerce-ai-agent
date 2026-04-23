using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.Tools.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Ollama;

namespace ECommerce.AgentAPI.Infrastructure.LLM.Ollama;

/// <summary>
/// Monta o <see cref="Microsoft.SemanticKernel.Kernel"/> com Ollama local e plugins de e-commerce.
/// </summary>
public sealed class OllamaKernelFactory
{
    private readonly IConfiguration _configuration;
    private readonly ToolApprovalService _toolApproval;

    public OllamaKernelFactory(IConfiguration configuration, ToolApprovalService toolApproval)
    {
        _configuration = configuration;
        _toolApproval = toolApproval;
    }

    public Microsoft.SemanticKernel.Kernel CreateKernel(IECommerceApi ecommerceApi, Guid sessionId)
    {
        ArgumentNullException.ThrowIfNull(ecommerceApi);

        var model = _configuration["LLM:Ollama:Model"] ?? "llama3.2";
        var baseUrl = _configuration["LLM:Ollama:BaseUrl"] ?? "http://localhost:11434";
        if (!Uri.TryCreate(baseUrl.Trim(), UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException($"Configuracao invalida para LLM:Ollama:BaseUrl: '{baseUrl}'.");
        }

        var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
#pragma warning disable SKEXP0070
        builder.AddOllamaChatCompletion(modelId: model, endpoint: endpoint);
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

    /// <summary>
    /// O teto de <see cref="OllamaPromptExecutionSettings.NumPredict"/> é ajustado em
    /// <see cref="OllamaLLMService"/> (ex.: 512) para respostas mais rápidas no Ollama local.
    /// </summary>
    public OllamaPromptExecutionSettings CreatePromptExecutionSettings() =>
        new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = 0.3f,
            NumPredict = 1024
        };
}
