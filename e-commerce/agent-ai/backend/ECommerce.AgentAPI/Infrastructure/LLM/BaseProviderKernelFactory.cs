using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.Application.Agents.Routing;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.LLM.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.LLM;

public abstract class BaseProviderKernelFactory : IKernelFactory
{
    private readonly ToolApprovalService _toolApproval;
    private readonly IPluginFactory _pluginFactory;
    private readonly IAgentExecutionContext _agentExecutionContext;
    protected IConfiguration Configuration { get; }

    protected BaseProviderKernelFactory(
        IConfiguration configuration,
        ToolApprovalService toolApproval,
        IPluginFactory pluginFactory,
        IAgentExecutionContext agentExecutionContext)
    {
        Configuration = configuration;
        _toolApproval = toolApproval;
        _pluginFactory = pluginFactory;
        _agentExecutionContext = agentExecutionContext;
    }

    public virtual Microsoft.SemanticKernel.Kernel CreateKernel(
        string sessionId,
        IServiceProvider requestServices)
    {
        ArgumentNullException.ThrowIfNull(requestServices);

        var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
        var model = _agentExecutionContext.CurrentProfile?.Model;
        ConfigureProviderChatCompletion(builder, model);

        builder.Services.AddSingleton(_toolApproval);
        builder.Services.AddSingleton<IFunctionInvocationFilter, ApprovalFilter>();
        builder.AddRegisteredToolPlugins(
            _pluginFactory,
            requestServices,
            _agentExecutionContext.CurrentProfile?.EnabledPlugins);

        var kernel = builder.Build();
        kernel.Data[AgentKernelDataKeys.SessionId] = sessionId;
        kernel.Data[AgentKernelDataKeys.AutomaticToolInvocations] = new List<RecordedToolInvocation>();
        return kernel;
    }

    protected abstract void ConfigureProviderChatCompletion(IKernelBuilder builder, string? modelOverride);
}
