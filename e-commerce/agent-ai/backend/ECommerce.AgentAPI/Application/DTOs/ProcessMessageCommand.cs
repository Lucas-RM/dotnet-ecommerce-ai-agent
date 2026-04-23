namespace ECommerce.AgentAPI.Application.DTOs;

/// <summary> Entrada do <see cref="UseCases.ProcessUserMessageUseCase"/>. </summary>
public sealed class ProcessMessageCommand
{
    public string SessionId { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? JwtToken { get; init; }
}
