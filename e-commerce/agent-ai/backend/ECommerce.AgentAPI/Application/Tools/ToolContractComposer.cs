using ECommerce.AgentAPI.Domain.ValueObjects;

namespace ECommerce.AgentAPI.Application.Tools;

public static class ToolContractComposer
{
    public static IReadOnlyList<ToolDefinition> Compose(
        ToolCatalog catalog,
        IReadOnlyCollection<string>? allowedTools = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var allowSet = allowedTools is null
            ? null
            : new HashSet<string>(allowedTools, StringComparer.Ordinal);

        var byName = catalog.GetAll()
            .GroupBy(t => t.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);

        return ToolRegistry.GetDefinitions()
            .Where(d => allowSet is null || allowSet.Contains(d.Name))
            .Select(definition => ComposeDefinition(definition, byName))
            .OrderBy(definition => definition.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static ToolDefinition ComposeDefinition(
        ToolDefinition definition,
        IReadOnlyDictionary<string, ITool> byName)
    {
        byName.TryGetValue(definition.Name, out var tool);

        return new ToolDefinition
        {
            Name = definition.Name,
            Description = definition.Description,
            Parameters = definition.Parameters
                .Select(parameter => new ToolParameter
                {
                    Name = parameter.Name,
                    Type = parameter.Type,
                    Description = parameter.Description,
                    Required = parameter.Required
                })
                .ToList(),
            RequiresApproval = tool?.RequiresApproval ?? false,
            DataType = tool?.DataType,
            Version = tool?.Version ?? ToolContractVersion.Current
        };
    }
}
