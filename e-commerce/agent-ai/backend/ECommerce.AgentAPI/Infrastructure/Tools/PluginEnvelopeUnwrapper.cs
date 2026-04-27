using System.Text.Json;

namespace ECommerce.AgentAPI.Infrastructure.Tools;

/// <summary>Desembrulha a mesma cadeia JSON que o <c>ToolExecutorService</c> (envelope de API, erros, JSON cru).</summary>
public static class PluginEnvelopeUnwrapper
{
    public static (bool Success, JsonElement? Data, string? Error) Unwrap(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return (true, null, null);

        JsonDocument? doc = null;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return (true, null, null);
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return (true, root.Clone(), null);

            var hasSuccess = root.TryGetProperty("success", out var successEl)
                             && successEl.ValueKind is JsonValueKind.True or JsonValueKind.False;
            if (!hasSuccess)
                return (true, root.Clone(), null);

            var success = successEl.GetBoolean();
            string? error = null;

            if (!success)
            {
                if (root.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
                    error = msgEl.GetString();

                if (string.IsNullOrWhiteSpace(error)
                    && root.TryGetProperty("errors", out var errsEl)
                    && errsEl.ValueKind == JsonValueKind.Array)
                {
                    var first = errsEl.EnumerateArray()
                        .FirstOrDefault(e => e.ValueKind == JsonValueKind.String);
                    if (first.ValueKind == JsonValueKind.String)
                        error = first.GetString();
                }
            }

            JsonElement? data = null;
            if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind != JsonValueKind.Null)
                data = dataEl.Clone();

            return (success, data, error);
        }
    }
}
