using ECommerce.AgentAPI.Application.DTOs;
using ECommerce.AgentAPI.Application.UseCases;
using ECommerce.AgentAPI.Models;

namespace ECommerce.AgentAPI.Application.Agents;

/// <summary> Fachada sobre <see cref="ProcessUserMessageUseCase"/> (testes, canais fora de HTTP). </summary>
public sealed class ChatAgent
{
    private readonly ProcessUserMessageUseCase _useCase;

    public ChatAgent(ProcessUserMessageUseCase useCase) => _useCase = useCase;

    public Task<ChatProcessResult> ProcessMessageAsync(
        ProcessMessageCommand command,
        CancellationToken cancellationToken = default) =>
        _useCase.ExecuteAsync(command, cancellationToken);

    public async Task<string> ExecuteAsync(string sessionId, string input, string jwt, CancellationToken cancellationToken = default)
    {
        var r = await _useCase
            .ExecuteAsync(
                new ProcessMessageCommand
                {
                    SessionId = sessionId,
                    Message = input,
                    JwtToken = jwt
                },
                cancellationToken)
            .ConfigureAwait(false);
        return r.Response.Reply;
    }

    public async Task<string> GetReplyTextOnlyAsync(
        ProcessMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        var r = await _useCase.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
        return r.Response.Reply;
    }
}
