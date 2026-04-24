using ECommerce.AgentAPI.Application.Abstractions;
using ECommerce.AgentAPI.Application.Agents;
using ECommerce.AgentAPI.Application.Approval;
using ECommerce.AgentAPI.Application.Chat;
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

/// <summary>
/// Orquestra: memória → LLM (via <see cref="ILLMFactory"/>) → aprovação (<see cref="IToolApprovalService"/>) →
/// tools (<see cref="IToolExecutor"/>) → envelope por tool (<see cref="ToolEnvelopeRegistry"/>).
/// A montagem do <see cref="ChatResponse"/> é centralizada em <see cref="BuildResponse"/>, de modo que
/// acrescentar uma tool nova não exige alterações neste arquivo: basta um novo <c>IToolEnvelopeBuilder</c>.
/// </summary>
public sealed class ProcessUserMessageUseCase
{
    private const string DefaultToolErrorMessage = "Falha ao executar a ação.";
    private const string DefaultApprovedToolErrorMessage = "Não foi possível executar a ação confirmada. Tente novamente.";
    private const string SessionMissingMessage = "Não foi possível continuar a conversa. Atualize a página e tente de novo.";
    private const string CancelPendingMessage = "Ok, cancelei. Posso ajudar com mais alguma coisa?";
    private const string AmbiguousApprovalMessage = "Preciso de uma confirmação objetiva: responda **sim** para prosseguir ou **não** para cancelar.";

    private readonly ILLMFactory _llmFactory;
    private readonly IToolExecutor _tools;
    private readonly IToolApprovalService _approval;
    private readonly IMemoryService _memory;
    private readonly IOptions<AgentOptions> _options;
    private readonly IChatErrorHandler _errorHandler;
    private readonly ToolEnvelopeRegistry _envelopes;

    public ProcessUserMessageUseCase(
        ILLMFactory llmFactory,
        IToolExecutor tools,
        IToolApprovalService approval,
        IMemoryService memory,
        IOptions<AgentOptions> options,
        IChatErrorHandler errorHandler,
        ToolEnvelopeRegistry envelopes)
    {
        _llmFactory = llmFactory;
        _tools = tools;
        _approval = approval;
        _memory = memory;
        _options = options;
        _errorHandler = errorHandler;
        _envelopes = envelopes;
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
            return new ChatProcessResult(
                StatusCodes.Status400BadRequest,
                BuildResponse(ChatEnvelope.TextOnly(SessionMissingMessage)));
        }

        var existing = await _approval.GetPendingAsync(sessionId).ConfigureAwait(false);
        if (existing is not null)
        {
            await PersistUserMessageAsync(sessionId, command.Message).ConfigureAwait(false);

            var classification = _approval.ClassifyUserResponse(command.Message);
            return classification switch
            {
                ApprovalClassification.Confirmed => await ExecuteApprovedToolAsync(existing, command, cancellationToken)
                    .ConfigureAwait(false),
                ApprovalClassification.Denied => await CancelPendingAsync(sessionId).ConfigureAwait(false),
                _ => AmbiguousApprovalResult(existing)
            };
        }

        _ = await _memory.GetHistoryAsync(sessionId).ConfigureAwait(false);
        var llm = _llmFactory.CreateFromConfig();

        await PersistUserMessageAsync(sessionId, command.Message).ConfigureAwait(false);

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
            var call = response.ToolCall;
            call.SessionId = sessionId;

            if (_approval.RequiresApproval(call.Name))
            {
                return await RequestApprovalAsync(sessionId, call).ConfigureAwait(false);
            }

            var executionResult = await _tools
                .ExecuteAsync(call, command.JwtToken ?? string.Empty, cancellationToken)
                .ConfigureAwait(false);
            return await HandleToolResultAsync(sessionId, call, executionResult).ConfigureAwait(false);
        }

        return await HandleLlmTextResponseAsync(sessionId, response.Text).ConfigureAwait(false);
    }

    private async Task<ChatProcessResult> RequestApprovalAsync(string sessionId, ToolCall call)
    {
        var approvalMessage = ApprovalMessageBuilder.Build(call);
        await _approval
            .StorePendingAsync(
                new PendingApproval
                {
                    SessionId = sessionId,
                    ToolCall = call,
                    ApprovalMessage = approvalMessage,
                    CreatedAt = DateTime.UtcNow
                })
            .ConfigureAwait(false);

        return Ok(BuildResponse(
            ChatEnvelope.TextOnly(approvalMessage),
            requiresApproval: true,
            pendingToolName: call.Name));
    }

    private async Task<ChatProcessResult> HandleToolResultAsync(
        string sessionId,
        ToolCall call,
        ToolExecutionResult executionResult)
    {
        if (!executionResult.Success)
        {
            var error = executionResult.Error ?? DefaultToolErrorMessage;
            await PersistAssistantReplyAsync(sessionId, error).ConfigureAwait(false);
            return Ok(BuildResponse(ChatEnvelope.TextOnly(error)));
        }

        var envelope = _envelopes.BuildFor(call.Name, executionResult.Data);
        await PersistAssistantReplyAsync(sessionId, JoinIntroOutro(envelope)).ConfigureAwait(false);
        await PruneHistoryAsync(sessionId).ConfigureAwait(false);
        return Ok(BuildResponse(envelope));
    }

    private async Task<ChatProcessResult> HandleLlmTextResponseAsync(string sessionId, string? rawText)
    {
        // Caminho sem tool: o modelo pode colar JSON no texto apesar do prompt.
        // Mantemos `AssistantReplyFormatter` + `AssistantOutputGuard` apenas aqui, como rede de segurança.
        var text = AssistantOutputGuard.EnsureUserFacing(
            AssistantReplyFormatter.ToUserFriendly(rawText ?? string.Empty));
        await PersistAssistantReplyAsync(sessionId, text).ConfigureAwait(false);
        await PruneHistoryAsync(sessionId).ConfigureAwait(false);
        return Ok(BuildResponse(ChatEnvelope.TextOnly(text)));
    }

    private async Task<ChatProcessResult> ExecuteApprovedToolAsync(
        PendingApproval pending,
        ProcessMessageCommand command,
        CancellationToken cancellationToken)
    {
        var sessionId = command.SessionId;
        await _approval.ClearPendingAsync(sessionId).ConfigureAwait(false);

        pending.ToolCall.SessionId = sessionId;
        var executionResult = await _tools
            .ExecuteAsync(pending.ToolCall, command.JwtToken ?? string.Empty, cancellationToken)
            .ConfigureAwait(false);

        if (!executionResult.Success)
        {
            // Mantém a aprovação pendente para permitir nova tentativa do usuário.
            await _approval.StorePendingAsync(pending).ConfigureAwait(false);
            var error = executionResult.Error ?? DefaultApprovedToolErrorMessage;
            return Ok(BuildResponse(
                ChatEnvelope.TextOnly(error),
                requiresApproval: true,
                pendingToolName: pending.ToolCall.Name));
        }

        var envelope = _envelopes.BuildFor(pending.ToolCall.Name, executionResult.Data);
        await PersistAssistantReplyAsync(sessionId, JoinIntroOutro(envelope)).ConfigureAwait(false);
        await PruneHistoryAsync(sessionId).ConfigureAwait(false);
        return Ok(BuildResponse(envelope));
    }

    private async Task<ChatProcessResult> CancelPendingAsync(string sessionId)
    {
        await _approval.ClearPendingAsync(sessionId).ConfigureAwait(false);
        await PersistAssistantReplyAsync(sessionId, CancelPendingMessage).ConfigureAwait(false);
        return Ok(BuildResponse(ChatEnvelope.TextOnly(CancelPendingMessage)));
    }

    private static ChatProcessResult AmbiguousApprovalResult(PendingApproval pending) =>
        Ok(BuildResponse(
            ChatEnvelope.TextOnly(AmbiguousApprovalMessage),
            requiresApproval: true,
            pendingToolName: pending.ToolCall.Name));

    /// <summary>
    /// Único ponto de montagem do <see cref="ChatResponse"/> a partir de um <see cref="ChatEnvelope"/>.
    /// Garante o contrato (intro/outro/tool/data) para todos os ramos do fluxo.
    /// </summary>
    private static ChatResponse BuildResponse(
        ChatEnvelope envelope,
        bool requiresApproval = false,
        string? pendingToolName = null)
    {
        var toolInfo = string.IsNullOrWhiteSpace(envelope.ToolName)
            ? null
            : new ChatToolInfo
            {
                Name = envelope.ToolName!,
                DataType = envelope.DataType
            };

        return new ChatResponse
        {
            IntroMessage = envelope.IntroMessage,
            OutroMessage = envelope.OutroMessage,
            Tool = toolInfo,
            Data = envelope.Data,
            RequiresApproval = requiresApproval,
            PendingToolName = pendingToolName
        };
    }

    private static ChatProcessResult Ok(ChatResponse response) =>
        new(StatusCodes.Status200OK, response);

    /// <summary>
    /// Concatena apenas os textos (intro + outro) para persistência em memória.
    /// <c>Data</c> é descartado para não inflar o contexto enviado ao LLM em turnos seguintes.
    /// </summary>
    private static string JoinIntroOutro(ChatEnvelope envelope)
    {
        var parts = new[] { envelope.IntroMessage, envelope.OutroMessage }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join("\n", parts).Trim();
    }

    private Task PersistUserMessageAsync(string sessionId, string content) =>
        _memory.SaveMessageAsync(
            new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = MessageRole.User,
                Content = content,
                CreatedAt = DateTime.UtcNow
            });

    private Task PersistAssistantReplyAsync(string sessionId, string text) =>
        _memory.SaveMessageAsync(
            new ChatMessage
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                Role = MessageRole.Assistant,
                Content = text,
                CreatedAt = DateTime.UtcNow
            });

    private Task PruneHistoryAsync(string sessionId) =>
        _memory.PruneHistoryAsync(sessionId, _options.Value.MaxConversationTurns);
}
