using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Application.Tools;
using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.Infrastructure.Approval;
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

    public OpenAIKernelFactory(IConfiguration configuration, ToolApprovalService toolApproval)
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

        foreach (var pluginType in ToolRegistry.GetPluginTypes())
        {
            var plugin = CreatePluginInstance(pluginType, ecommerceApi);
            builder.Plugins.AddFromObject(plugin, pluginType.Name);
        }

        var kernel = builder.Build();
        kernel.Data[AgentKernelDataKeys.SessionId] = sessionId;
        kernel.Data[AgentKernelDataKeys.AutomaticToolInvocations] = new List<RecordedToolInvocation>();
        return kernel;
    }

    private static object CreatePluginInstance(Type pluginType, IECommerceApi ecommerceApi)
    {
        var ctor = pluginType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"Plugin '{pluginType.FullName}' sem construtor público.");

        var args = ctor.GetParameters()
            .Select(p => ResolvePluginDependency(p.ParameterType, ecommerceApi, pluginType))
            .ToArray();

        var instance = Activator.CreateInstance(pluginType, args);
        return instance ?? throw new InvalidOperationException(
            $"Não foi possível instanciar o plugin '{pluginType.FullName}'.");
    }

    private static object ResolvePluginDependency(Type dependencyType, IECommerceApi ecommerceApi, Type pluginType)
    {
        if (dependencyType.IsInstanceOfType(ecommerceApi))
            return ecommerceApi;

        throw new InvalidOperationException(
            $"Plugin '{pluginType.FullName}' possui dependência não suportada no construtor: '{dependencyType.FullName}'.");
    }

    public OpenAIPromptExecutionSettings CreatePromptExecutionSettings() =>
        new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Temperature = 0.3,
            MaxTokens = 1024
        };
}
