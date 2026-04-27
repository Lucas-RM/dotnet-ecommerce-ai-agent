using System.Text.Json;

namespace ECommerce.AgentAPI.Application.Tools.Serialization;

/// <summary>
/// Ponto único de (des)serialização de payloads <c>data</c> devolvidos pela API de e-commerce
/// às <see cref="ITool"/>. Nomes em camelCase, case-insensitive alinhado ao tráfego HTTP padrão.
/// </summary>
public static class ToolPayloadJson
{
    /// <summary>Versão lógica de payload documentada em <c>Application.Tools.Payloads.V1</c>.</summary>
    public const int DefaultPayloadVersion = 1;

    public static JsonSerializerOptions DataSerializerOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static T? Deserialize<T>(JsonElement? el)
    {
        if (el is not { } value
            || value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
        {
            return default;
        }

        return value.Deserialize<T>(DataSerializerOptions);
    }

    public static int ArrayLength(JsonElement? el, string name)
    {
        if (el is null or { ValueKind: not JsonValueKind.Object })
        {
            return 0;
        }

        if (!el.Value.TryGetProperty(name, out var prop) || prop.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        return prop.GetArrayLength();
    }
}
