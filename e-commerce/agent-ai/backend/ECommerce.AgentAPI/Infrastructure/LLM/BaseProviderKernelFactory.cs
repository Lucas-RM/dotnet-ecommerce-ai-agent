using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.LLM.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.LLM;

public abstract class BaseProviderKernelFactory : IKernelFactory
{
    private readonly ToolApprovalService _toolApproval;
    private readonly IPluginFactory _pluginFactory;
    protected IConfiguration Configuration { get; }

    protected BaseProviderKernelFactory(
        IConfiguration configuration,
        ToolApprovalService toolApproval,
        IPluginFactory pluginFactory)
    {
        Configuration = configuration;
        _toolApproval = toolApproval;
        _pluginFactory = pluginFactory;
    }

    public virtual Microsoft.SemanticKernel.Kernel CreateKernel(
        string sessionId,
        IServiceProvider requestServices)
    {
        if (!Guid.TryParse(sessionId, out _))
        {
            throw new ArgumentException("SessionId inválido.", nameof(sessionId));
        }

        ArgumentNullException.ThrowIfNull(requestServices);

        var builder = Microsoft.SemanticKernel.Kernel.CreateBuilder();
        ConfigureProviderChatCompletion(builder);

        builder.Services.AddSingleton(_toolApproval);
        builder.Services.AddSingleton<IFunctionInvocationFilter, ApprovalFilter>();
        builder.AddRegisteredToolPlugins(_pluginFactory, requestServices);

        var kernel = builder.Build();
        kernel.Data[AgentKernelDataKeys.SessionId] = sessionId;
        kernel.Data[AgentKernelDataKeys.AutomaticToolInvocations] = new List<RecordedToolInvocation>();
        return kernel;
    }

    protected abstract void ConfigureProviderChatCompletion(IKernelBuilder builder);
}
