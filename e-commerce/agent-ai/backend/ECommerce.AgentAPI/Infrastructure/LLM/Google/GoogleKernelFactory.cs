using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.LLM.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.LLM.Google;

/// <summary>
/// Monta o <see cref="Microsoft.SemanticKernel.Kernel"/> com Google AI (Gemini) e os plugins de e-commerce.
/// Mesmo padrão da fábrica de kernel do OpenAI (<see cref="ECommerce.AgentAPI.Infrastructure.LLM.OpenAI.OpenAIKernelFactory"/>):
/// recebe dependências scoped via requestServices
/// por parâmetro e instancia filtros/plugins manualmente, evitando resolver serviços scoped a partir do root provider
/// (o que dispararia <c>InvalidOperationException</c> "Cannot resolve scoped service ... from root provider.").
/// </summary>
public sealed class GoogleKernelFactory : IKernelFactory
{
    private readonly IConfiguration _configuration;
    private readonly ToolApprovalService _toolApproval;
    private readonly IPluginFactory _pluginFactory;

    public GoogleKernelFactory(
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
        ArgumentNullException.ThrowIfNull(requestServices);

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

        builder.AddRegisteredToolPlugins(_pluginFactory, requestServices);

        var kernel = builder.Build();
        kernel.Data[AgentKernelDataKeys.SessionId] = sessionId;
        kernel.Data[AgentKernelDataKeys.AutomaticToolInvocations] = new List<RecordedToolInvocation>();
        return kernel;
    }
}
