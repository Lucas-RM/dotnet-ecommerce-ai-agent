using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.AgentAPI.Application.Tools;

/// <summary>
/// Auto-registra todas as implementações de <see cref="ITool"/> encontradas por scan de assembly +
/// registra o <see cref="ToolCatalog"/> como singleton. Segue o mesmo padrão do
/// <c>ToolEnvelopeServiceCollectionExtensions</c>: adicionar tool = criar a classe, sem mexer no DI.
/// </summary>
public static class ToolCatalogServiceCollectionExtensions
{
    public static IServiceCollection AddToolCatalog(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var targets = assemblies is { Length: > 0 } ? assemblies : [Assembly.GetCallingAssembly()];
        foreach (var assembly in targets)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type is { IsClass: true, IsAbstract: false }
                    && typeof(ITool).IsAssignableFrom(type))
                {
                    services.AddSingleton(typeof(ITool), type);
                }
            }
        }

        services.AddSingleton<ToolCatalog>();
        return services;
    }
}
