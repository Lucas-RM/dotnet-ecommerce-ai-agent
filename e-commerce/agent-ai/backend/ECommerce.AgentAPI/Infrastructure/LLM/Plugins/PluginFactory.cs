namespace ECommerce.AgentAPI.Infrastructure.LLM.Plugins;

public sealed class PluginFactory : IPluginFactory
{
    public object CreatePlugin(Type pluginType, IServiceProvider requestServices)
    {
        ArgumentNullException.ThrowIfNull(pluginType);
        ArgumentNullException.ThrowIfNull(requestServices);

        var ctor = pluginType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"Plugin '{pluginType.FullName}' sem construtor público.");

        var args = ctor.GetParameters()
            .Select(parameter => ResolvePluginDependency(parameter.ParameterType, requestServices, pluginType))
            .ToArray();

        var instance = Activator.CreateInstance(pluginType, args);
        return instance ?? throw new InvalidOperationException(
            $"Não foi possível instanciar o plugin '{pluginType.FullName}'.");
    }

    private static object ResolvePluginDependency(
        Type dependencyType,
        IServiceProvider requestServices,
        Type pluginType)
    {
        var dependency = requestServices.GetService(dependencyType);
        if (dependency is not null)
        {
            return dependency;
        }

        throw new InvalidOperationException(
            $"Plugin '{pluginType.FullName}' possui dependência não resolvida no construtor: '{dependencyType.FullName}'. " +
            "Registre a dependência no DI da API.");
    }
}
