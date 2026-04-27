using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Application.Tools;
using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.Infrastructure.Approval;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.LLM.Google;

/// <summary>
/// Monta o <see cref="Microsoft.SemanticKernel.Kernel"/> com Google AI (Gemini) e os plugins de e-commerce.
/// Mesmo padrão da fábrica de kernel do OpenAI (<see cref="ECommerce.AgentAPI.Infrastructure.LLM.OpenAI.OpenAIKernelFactory"/>):
/// recebe dependências scoped (ex.: <see cref="IECommerceApi"/>)
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
}
