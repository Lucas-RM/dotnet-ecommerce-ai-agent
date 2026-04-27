using System.ComponentModel;
using System.Reflection;
using ECommerce.AgentAPI.Domain.ValueObjects;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Application.Tools;

/// <summary>
/// Projeta o schema das tools (nome, descrição, parâmetros) diretamente dos plugins do
/// Semantic Kernel, lendo <see cref="KernelFunctionAttribute"/> e <see cref="DescriptionAttribute"/>
/// via reflection. A fonte de verdade passa a ser o próprio <c>[KernelFunction]</c> do plugin —
/// adicionar uma tool exige apenas declará-la no plugin; este registry descobre sozinho.
/// </summary>
public static class ToolRegistry
{
    private static readonly Lazy<IReadOnlyList<Type>> CachedPluginTypes =
        new(DiscoverPluginTypes, LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly Lazy<IReadOnlyList<ToolDefinition>> Cached =
        new(BuildDefinitions, LazyThreadSafetyMode.ExecutionAndPublication);

    public static IReadOnlyList<Type> GetPluginTypes() => CachedPluginTypes.Value;

    public static IReadOnlyList<ToolDefinition> GetDefinitions() => Cached.Value;

    private static IReadOnlyList<ToolDefinition> BuildDefinitions() =>
        GetPluginTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Select(method => (Method: method, Attr: method.GetCustomAttribute<KernelFunctionAttribute>()))
            .Where(x => x.Attr is not null)
            .Select(x => new ToolDefinition
            {
                Name = ResolveFunctionName(x.Method, x.Attr!),
                Description = x.Method.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
                Parameters = x.Method
                    .GetParameters()
                    .Select(ToToolParameter)
                    .ToList()
            })
            .OrderBy(d => d.Name, StringComparer.Ordinal)
            .ToList();

    private static IReadOnlyList<Type> DiscoverPluginTypes()
    {
        var assembly = typeof(ToolRegistry).Assembly;
        return assembly
            .GetTypes()
            .Where(IsPluginType)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static bool IsPluginType(Type type)
    {
        if (type is not { IsClass: true, IsAbstract: false })
            return false;

        var hasKernelFunctions = type
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Any(m => m.GetCustomAttribute<KernelFunctionAttribute>() is not null);
        if (!hasKernelFunctions)
            return false;

        if (type.GetCustomAttribute<ToolPluginAttribute>() is not null)
            return true;

        return type.Name.EndsWith("Plugin", StringComparison.Ordinal);
    }

    private static string ResolveFunctionName(MethodInfo method, KernelFunctionAttribute attr) =>
        !string.IsNullOrWhiteSpace(attr.Name)
            ? attr.Name!
            : StripAsyncSuffix(method.Name);

    private static string StripAsyncSuffix(string name) =>
        name.EndsWith("Async", StringComparison.Ordinal) && name.Length > "Async".Length
            ? name[..^"Async".Length]
            : name;

    private static ToolParameter ToToolParameter(ParameterInfo p) => new()
    {
        Name = p.Name ?? string.Empty,
        Type = MapJsonSchemaType(p.ParameterType),
        Description = p.GetCustomAttribute<DescriptionAttribute>()?.Description,
        Required = !p.HasDefaultValue
    };

    private static string MapJsonSchemaType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return Type.GetTypeCode(underlying) switch
        {
            TypeCode.Boolean => "boolean",
            TypeCode.Byte or TypeCode.SByte
                or TypeCode.Int16 or TypeCode.UInt16
                or TypeCode.Int32 or TypeCode.UInt32
                or TypeCode.Int64 or TypeCode.UInt64 => "integer",
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal => "number",
            TypeCode.String or TypeCode.Char => "string",
            _ => "string"
        };
    }
}
