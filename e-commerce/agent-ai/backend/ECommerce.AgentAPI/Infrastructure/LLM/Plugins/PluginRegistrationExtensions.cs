using ECommerce.AgentAPI.Application.Tools;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.LLM.Plugins;

public static class PluginRegistrationExtensions
{
    public static void AddRegisteredToolPlugins(
        this IKernelBuilder builder,
        IPluginFactory pluginFactory,
        IServiceProvider requestServices,
        IReadOnlyCollection<string>? allowedPlugins = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(pluginFactory);
        ArgumentNullException.ThrowIfNull(requestServices);

        var allowSet = allowedPlugins is null
            ? null
            : new HashSet<string>(allowedPlugins, StringComparer.OrdinalIgnoreCase);

        foreach (var pluginType in ToolRegistry.GetPluginTypes())
        {
            if (allowSet is not null && !allowSet.Contains(pluginType.Name))
                continue;

            var plugin = pluginFactory.CreatePlugin(pluginType, requestServices);
            builder.Plugins.AddFromObject(plugin, pluginType.Name);
        }
    }
}
