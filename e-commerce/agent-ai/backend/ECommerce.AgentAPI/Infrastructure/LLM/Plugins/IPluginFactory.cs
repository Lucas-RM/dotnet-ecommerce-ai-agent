namespace ECommerce.AgentAPI.Infrastructure.LLM.Plugins;

public interface IPluginFactory
{
    object CreatePlugin(Type pluginType, IServiceProvider requestServices);
}
