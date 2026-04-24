using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.AgentAPI.Application.Chat;

/// <summary>
/// Extensões de DI para auto-registrar todos os <see cref="IToolEnvelopeBuilder"/> encontrados por scan de assembly.
/// Adicionar tool nova = basta criar a classe no assembly escaneado; nenhuma alteração no <c>DependencyInjection.cs</c>.
/// </summary>
public static class ToolEnvelopeServiceCollectionExtensions
{
    public static IServiceCollection AddToolEnvelopeBuilders(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var targets = assemblies is { Length: > 0 } ? assemblies : [Assembly.GetCallingAssembly()];
        foreach (var assembly in targets)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type is { IsClass: true, IsAbstract: false }
                    && typeof(IToolEnvelopeBuilder).IsAssignableFrom(type))
                {
                    services.AddSingleton(typeof(IToolEnvelopeBuilder), type);
                }
            }
        }

        services.AddSingleton<ToolEnvelopeRegistry>();
        return services;
    }
}
