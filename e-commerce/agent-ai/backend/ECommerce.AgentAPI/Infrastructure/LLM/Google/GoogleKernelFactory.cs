using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.LLM;
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
public sealed class GoogleKernelFactory : BaseProviderKernelFactory
{
    public GoogleKernelFactory(
        IConfiguration configuration,
        ToolApprovalService toolApproval,
        IPluginFactory pluginFactory)
        : base(configuration, toolApproval, pluginFactory)
    {
    }

    protected override void ConfigureProviderChatCompletion(IKernelBuilder builder)
    {
        var modelId = Configuration["LLM:Google:Model"]
            ?? throw new InvalidOperationException("Configuração ausente: LLM:Google:Model.");
        var apiKey = Configuration["LLM:Google:ApiKey"]
            ?? throw new InvalidOperationException("Configuração ausente: LLM:Google:ApiKey.");

#pragma warning disable SKEXP0070
        builder.AddGoogleAIGeminiChatCompletion(modelId: modelId, apiKey: apiKey);
#pragma warning restore SKEXP0070
    }
}
