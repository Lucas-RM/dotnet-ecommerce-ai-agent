using ECommerce.AgentAPI.Application.Abstractions;
using ECommerce.AgentAPI.Application.Agents;
using ECommerce.AgentAPI.Application.Approval;
using ECommerce.AgentAPI.Application.DTOs;
using ECommerce.AgentAPI.Application.Options;
using ECommerce.AgentAPI.Application.Tools;
using ECommerce.AgentAPI.Domain.Entities;
using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.Infrastructure.Formatting;
using ECommerce.AgentAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ECommerce.AgentAPI.Application.UseCases;

/// <summary> Orquestra: memória → LLM (via ILLMFactory) → aprovação (IToolApprovalService) → tools (IToolExecutor). </summary>
public sealed class ProcessUserMessageUseCase
{
    private readonly ILLMFactory _llmFactory;
    private readonly IToolExecutor _tools;
    private readonly IToolApprovalService _approval;
    private readonly IMemoryService _memory;
    private readonly IOptions<AgentOptions> _options;
    private readonly IChatErrorHandler _errorHandler;

    public ProcessUserMessageUseCase(
        ILLMFactory llmFactory,
        IToolExecutor tools,
        IToolApprovalService approval,
        IMemoryService memory,
        IOptions<AgentOptions> options,
        IChatErrorHandler errorHandler)
    {
        _llmFactory = llmFactory;
        _tools = tools;
        _approval = approval;
        _memory = memory;
        _options = options;
        _errorHandler = errorHandler;
    }

    public async Task<ChatProcessResult> ExecuteAsync(
        ProcessMessageCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await RunAsync(command, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return _errorHandler.MapToProcessResult(ex);
        }
    }

    private async Task<ChatProcessResult> RunAsync(
        ProcessMessageCommand command,
        CancellationToken cancellationToken)
    {
        var sessionId = command.SessionId;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return new ChatProcessResult(StatusCodes.Status400BadRequest,
                new ChatResponse
                {
                    Reply = "Não foi possível continuar a conversa. Atualize a página e tente de novo.",
                    RequiresApproval = false
                });
        }

        var existing = await _approval.GetPendingAsync(sessionId).ConfigureAwait(false);
        if (existing is not null)
        {
            await _memory
                .SaveMessageAsync(
                    new ChatMessage
                    {
                        Id = Guid.NewGuid(),
                        SessionId = sessionId,
                        Role = MessageRole.User,
                        Content = command.Message,
                        CreatedAt = DateTime.UtcNow
                    })
                .ConfigureAwait(false);

            var c = _approval.ClassifyUserResponse(command.Message);
            return c switch
            {
                ApprovalClassification.Confirmed => await ExecuteApprovedToolAsync(existing, command, cancellationToken)
                    .ConfigureAwait(false),
                ApprovalClassification.Denied => await CancelPendingAsync(sessionId, cancellationToken).ConfigureAwait(false),
                _ => AmbiguousApprovalResult(existing)
            };
        }

        _ = await _memory.GetHistoryAsync(sessionId).ConfigureAwait(false);
        var llm = _llmFactory.CreateFromConfig();

        await _memory
            .SaveMessageAsync(
                new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    Role = MessageRole.User,
                    Content = command.Message,
                    CreatedAt = DateTime.UtcNow
                })
            .ConfigureAwait(false);

        var response = await llm
            .GenerateAsync(
                new LLMRequest
                {
                    SessionId = sessionId,
                    Input = command.Message,
                    History = await _memory.GetHistoryAsync(sessionId).ConfigureAwait(false),
                    Tools = [.. ToolRegistry.GetDefinitions()],
                    SystemPrompt = AgentSystemPrompt.Text,
                    Temperature = 0.3f,
                    MaxTokens = 1024
                },
                cancellationToken)
            .ConfigureAwait(false);

        if (response.HasToolCall && response.ToolCall is not null)
        {
            var t = response.ToolCall;
            t.SessionId = sessionId;
            if (_approval.RequiresApproval(t.Name))
            {
                var msg = ApprovalMessageBuilder.Build(t);
                await _approval
                    .StorePendingAsync(
                        new PendingApproval
                        {
                            SessionId = sessionId,
                            ToolCall = t,
                            ApprovalMessage = msg,
                            CreatedAt = DateTime.UtcNow
                        })
                    .ConfigureAwait(false);
                return Ok(
                    new ChatResponse
                    {
                        Reply = msg,
                        RequiresApproval = true,
                        PendingToolName = t.Name
                    });
            }

            var jwt = command.JwtToken ?? string.Empty;
            var tr = await _tools.ExecuteAsync(t, jwt, cancellationToken).ConfigureAwait(false);
            if (!tr.Success)
            {
                var err = tr.Error ?? "Falha ao executar a ação.";
                await PersistAssistantReplyAsync(sessionId, err).ConfigureAwait(false);
                return Ok(
                    new ChatResponse
                    {
                        Reply = AssistantOutputGuard.EnsureUserFacing(AssistantReplyFormatter.ToUserFriendly(err))
                    });
            }

            var outText = AssistantOutputGuard.EnsureUserFacing(AssistantReplyFormatter.ToUserFriendly(tr.Output));
            await PersistAssistantReplyAsync(sessionId, outText).ConfigureAwait(false);
            await _memory
                .PruneHistoryAsync(sessionId, _options.Value.MaxConversationTurns)
                .ConfigureAwait(false);
            return Ok(new ChatResponse { Reply = outText, RequiresApproval = false });
        }

        var text = AssistantOutputGuard.EnsureUserFacing(AssistantReplyFormatter.ToUserFriendly(response.Text ?? string.Empty));
        await PersistAssistantReplyAsync(sessionId, text).ConfigureAwait(false);
        await _memory
            .PruneHistoryAsync(sessionId, _options.Value.MaxConversationTurns)
            .ConfigureAwait(false);
        return Ok(new ChatResponse { Reply = text, RequiresApproval = false });
    }

    private async Task<ChatProcessResult> ExecuteApprovedToolAsync(
        PendingApproval pending,
        ProcessMessageCommand command,
        CancellationToken cancellationToken)
    {
        var sessionId = command.SessionId;
        await _approval.ClearPendingAsync(sessionId).ConfigureAwait(false);

        pending.ToolCall.SessionId = sessionId;
        var tr = await _tools
            .ExecuteAsync(pending.ToolCall, command.JwtToken ?? string.Empty, cancellationToken)
            .ConfigureAwait(false);
        if (!tr.Success)
        {
            await _approval.StorePendingAsync(pending).ConfigureAwait(false);
            var err = tr.Error ?? "Não foi possível executar a ação confirmada. Tente novamente.";
            return Ok(
                new ChatResponse
                {
                    Reply = AssistantOutputGuard.EnsureUserFacing(AssistantReplyFormatter.ToUserFriendly(err)),
                    RequiresApproval = true,
                    PendingToolName = pending.ToolCall.Name
                });
        }

        var outText = AssistantOutputGuard.EnsureUserFacing(AssistantReplyFormatter.ToUserFriendly(tr.Output));
        await PersistAssistantReplyAsync(sessionId, outText).ConfigureAwait(false);
        await _memory
            .PruneHistoryAsync(sessionId, _options.Value.MaxConversationTurns)
            .ConfigureAwait(false);
        return Ok(new ChatResponse { Reply = outText, RequiresApproval = false });
    }

    private async Task<ChatProcessResult> CancelPendingAsync(string sessionId, CancellationToken _)
    {
        await _approval.ClearPendingAsync(sessionId).ConfigureAwait(false);
        const string deny = "Ok, cancelei. Posso ajudar com mais alguma coisa?";
        await PersistAssistantReplyAsync(sessionId, deny).ConfigureAwait(false);
        return Ok(new ChatResponse { Reply = deny, RequiresApproval = false });
    }

    private static ChatProcessResult AmbiguousApprovalResult(PendingApproval p) => Ok(
        new ChatResponse
        {
            Reply = "Preciso de uma confirmação objetiva: responda **sim** para prosseguir ou **não** para cancelar.",
            RequiresApproval = true,
            PendingToolName = p.ToolCall.Name
        });

    private static ChatProcessResult Ok(ChatResponse r) => new(StatusCodes.Status200OK, r);

    private async Task PersistAssistantReplyAsync(string sessionId, string text) =>
        await _memory
            .SaveMessageAsync(
                new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    SessionId = sessionId,
                    Role = MessageRole.Assistant,
                    Content = text,
                    CreatedAt = DateTime.UtcNow
                })
            .ConfigureAwait(false);
}
