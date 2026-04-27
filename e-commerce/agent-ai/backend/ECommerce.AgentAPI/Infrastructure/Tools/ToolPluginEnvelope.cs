namespace ECommerce.AgentAPI.Infrastructure.Tools;

internal sealed record ToolPluginEnvelope<T>(
    bool Success,
    T? Data,
    string? Message,
    IReadOnlyList<string>? Errors,
    string Version = ToolPluginEnvelopeVersion.Current);

internal static class ToolPluginEnvelopeVersion
{
    public const string Current = "1.0.0";
}

internal static class ToolPluginEnvelopeFactory
{
    public static ToolPluginEnvelope<T> Success<T>(T data) =>
        new(
            Success: true,
            Data: data,
            Message: null,
            Errors: null);

    public static ToolPluginEnvelope<object?> Failure(string message, params string[] errors) =>
        new(
            Success: false,
            Data: null,
            Message: message,
            Errors: errors is { Length: > 0 } ? errors : null);
}
