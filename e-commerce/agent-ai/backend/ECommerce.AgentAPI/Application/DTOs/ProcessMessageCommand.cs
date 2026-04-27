using System.Text.Json;

namespace ECommerce.AgentAPI.Application.DTOs;

/// <summary> Entrada do <see cref="UseCases.ProcessUserMessageUseCase"/>. </summary>
public sealed class ProcessMessageCommand
{
    public string SessionId { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? JwtToken { get; init; }

    public string? ClientVersion { get; init; }

    public string? Locale { get; init; }

    public string? Channel { get; init; }

    public JsonElement? Metadata { get; init; }

    public string? CorrelationId { get; init; }
}
