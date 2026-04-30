using ECommerce.AgentAPI.Domain.Entities;
using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.Infrastructure.Approval;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SkChat = Microsoft.SemanticKernel.ChatCompletion;

namespace ECommerce.AgentAPI.Infrastructure.LLM;

public abstract class BaseLLMService : ILLMService
{
    private readonly ToolApprovalService _toolApproval;
    private readonly ILogger _logger;

    protected BaseLLMService(ToolApprovalService toolApproval, ILogger logger)
    {
        _toolApproval = toolApproval;
        _logger = logger;
    }

    public async Task<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            return new LLMResponse
            {
                Text = "Sessão inválida para o motor de IA.",
                HasToolCall = false,
                FinishReason = "error"
            };
        }

        var sessionId = request.SessionId;
        var history = ToSkChatHistory(request);
        var kernel = CreateKernel(sessionId);
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var settings = CreatePromptExecutionSettings(request);

        IReadOnlyList<ChatMessageContent> contents;
        try
        {
            contents = await chat
                .GetChatMessageContentsAsync(history, settings, kernel, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            LogChatCompletionError(_logger, ex);
            throw;
        }

        if (_toolApproval.TryGetPending(sessionId, out var pending) && pending is not null)
        {
            return new LLMResponse
            {
                Text = null,
                HasToolCall = true,
                ToolCall = ToToolCall(pending, sessionId),
                RecordedToolExecutions = Array.Empty<RecordedToolInvocation>(),
                FinishReason = "tool_calls"
            };
        }

        var text = ExtractAssistantReply(contents);
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning(GetEmptyResponseWarningMessage());
            text = "Não obtive conteúdo do modelo. Tente de novo em instantes.";
        }

        return new LLMResponse
        {
            Text = text,
            HasToolCall = false,
            RecordedToolExecutions = KernelAutomaticToolListReader.ReadFromKernel(kernel),
            FinishReason = "stop"
        };
    }

    protected abstract Microsoft.SemanticKernel.Kernel CreateKernel(string sessionId);

    protected abstract PromptExecutionSettings CreatePromptExecutionSettings(LLMRequest request);

    protected abstract void LogChatCompletionError(ILogger logger, Exception ex);

    protected abstract string GetEmptyResponseWarningMessage();

    protected static SkChat.ChatHistory ToSkChatHistory(LLMRequest request)
    {
        var history = new SkChat.ChatHistory();
        if (!request.History.Any(m => m.Role == MessageRole.System) && !string.IsNullOrEmpty(request.SystemPrompt))
        {
            history.AddSystemMessage(request.SystemPrompt);
        }

        foreach (var message in request.History)
        {
            AppendMessage(history, message);
        }

        return history;
    }

    private static void AppendMessage(SkChat.ChatHistory history, ChatMessage message)
    {
        switch (message.Role)
        {
            case MessageRole.System:
                history.AddSystemMessage(message.Content);
                break;
            case MessageRole.User:
                history.AddUserMessage(message.Content);
                break;
            case MessageRole.Assistant:
                history.AddAssistantMessage(message.Content);
                break;
            case MessageRole.Tool:
                history.AddAssistantMessage(
                    string.IsNullOrEmpty(message.ToolName)
                        ? message.Content
                        : $"[{message.ToolName}] {message.Content}");
                break;
            default:
                history.AddUserMessage(message.Content);
                break;
        }
    }

    private static ToolCall ToToolCall(PendingToolCall pending, string sessionId)
    {
        var toolCall = new ToolCall { Name = pending.FunctionName, SessionId = sessionId };
        foreach (var kv in pending.Arguments)
        {
            toolCall.Arguments[kv.Key] = kv.Value ?? new object();
        }

        return toolCall;
    }

    private static string ExtractAssistantReply(IReadOnlyList<ChatMessageContent> contents)
    {
        for (var i = contents.Count - 1; i >= 0; i--)
        {
            if (contents[i].Role == AuthorRole.Assistant)
            {
                return contents[i].Content ?? string.Empty;
            }
        }

        return string.Empty;
    }
}
