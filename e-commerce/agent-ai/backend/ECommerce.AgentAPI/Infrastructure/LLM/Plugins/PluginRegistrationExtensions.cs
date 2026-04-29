using ECommerce.AgentAPI.Application.Tools;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.LLM.Plugins;

public static class PluginRegistrationExtensions
{
    public static void AddRegisteredToolPlugins(
        this IKernelBuilder builder,
        IPluginFactory pluginFactory,
        IServiceProvider requestServices)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(pluginFactory);
        ArgumentNullException.ThrowIfNull(requestServices);

        foreach (var pluginType in ToolRegistry.GetPluginTypes())
        {
            var plugin = pluginFactory.CreatePlugin(pluginType, requestServices);
            builder.Plugins.AddFromObject(plugin, pluginType.Name);
        }
    }
}
