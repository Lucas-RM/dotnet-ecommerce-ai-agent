using ECommerce.AgentAPI.Domain.Entities;
using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.LLM;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SkChat = Microsoft.SemanticKernel.ChatCompletion;

namespace ECommerce.AgentAPI.Infrastructure.LLM.OpenAI;

public sealed class OpenAILLMService : ILLMService
{
    private readonly OpenAIKernelFactory _kernelFactory;
    private readonly ToolApprovalService _toolApproval;
    private readonly ILogger<OpenAILLMService> _logger;
    private readonly IServiceProvider _requestServices;

    public OpenAILLMService(
        OpenAIKernelFactory kernelFactory,
        ToolApprovalService toolApproval,
        IServiceProvider requestServices,
        ILogger<OpenAILLMService> logger)
    {
        _kernelFactory = kernelFactory;
        _toolApproval = toolApproval;
        _requestServices = requestServices;
        _logger = logger;
    }

    public async Task<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId) || !Guid.TryParse(request.SessionId, out var sessionGuid))
        {
            return new LLMResponse
            {
                Text = "Sessão inválida para o motor de IA.",
                HasToolCall = false,
                FinishReason = "error"
            };
        }

        var key = request.SessionId;
        var history = ToSkChatHistory(request);
        var kernel = _kernelFactory.CreateKernel(sessionGuid.ToString(), _requestServices);
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var settings = _kernelFactory.CreatePromptExecutionSettings();
        if (request.Temperature > 0)
        {
            settings.Temperature = request.Temperature;
        }

        if (request.MaxTokens > 0)
        {
            settings.MaxTokens = request.MaxTokens;
        }

        IReadOnlyList<ChatMessageContent> contents;
        try
        {
            contents = await chat
                .GetChatMessageContentsAsync(history, settings, kernel, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no chat completion do OpenAI/SK.");
            throw;
        }

        if (_toolApproval.TryGetPending(key, out var p) && p is not null)
        {
            return new LLMResponse
            {
                Text = null,
                HasToolCall = true,
                ToolCall = ToToolCall(p, key),
                RecordedToolExecutions = Array.Empty<RecordedToolInvocation>(),
                FinishReason = "tool_calls"
            };
        }

        var text = ExtractAssistantReply(contents);
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("A API OpenAI devolveu resposta vazia após a invocação.");
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

    private static SkChat.ChatHistory ToSkChatHistory(LLMRequest request)
    {
        var h = new SkChat.ChatHistory();
        if (!request.History.Any(m => m.Role == MessageRole.System) && !string.IsNullOrEmpty(request.SystemPrompt))
        {
            h.AddSystemMessage(request.SystemPrompt);
        }

        foreach (var m in request.History)
        {
            AppendMessage(h, m);
        }

        return h;
    }

    private static void AppendMessage(SkChat.ChatHistory h, ChatMessage m)
    {
        switch (m.Role)
        {
            case MessageRole.System:
                h.AddSystemMessage(m.Content);
                break;
            case MessageRole.User:
                h.AddUserMessage(m.Content);
                break;
            case MessageRole.Assistant:
                h.AddAssistantMessage(m.Content);
                break;
            case MessageRole.Tool:
                h.AddAssistantMessage(string.IsNullOrEmpty(m.ToolName) ? m.Content : $"[{m.ToolName}] {m.Content}");
                break;
            default:
                h.AddUserMessage(m.Content);
                break;
        }
    }

    private static ToolCall ToToolCall(PendingToolCall pending, string sessionId)
    {
        var t = new ToolCall { Name = pending.FunctionName, SessionId = sessionId };
        foreach (var kv in pending.Arguments)
        {
            t.Arguments[kv.Key] = kv.Value ?? new object();
        }
        return t;
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
