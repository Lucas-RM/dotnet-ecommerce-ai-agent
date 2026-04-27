using ECommerce.AgentAPI.Domain.Entities;
using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.LLM;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using SkChat = Microsoft.SemanticKernel.ChatCompletion;

namespace ECommerce.AgentAPI.Infrastructure.LLM.Google;

/// <summary>
/// Implementação de <see cref="ILLMService"/> para Google AI (Gemini), seguindo o mesmo
/// fluxo do <see cref="OpenAI.OpenAILLMService"/>: monta <see cref="ChatHistory"/>,
/// invoca o chat completion via Semantic Kernel e extrai a <see cref="LLMResponse"/>.
/// </summary>
public sealed class GoogleLLMService : ILLMService
{
    private const double DefaultTemperature = 0.3;
    private const int DefaultMaxOutputTokens = 1024;

    private readonly GoogleKernelFactory _kernelFactory;
    private readonly IECommerceApi _eCommerceApi;
    private readonly ToolApprovalService _toolApproval;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleLLMService> _logger;

    public GoogleLLMService(
        GoogleKernelFactory kernelFactory,
        IECommerceApi eCommerceApi,
        ToolApprovalService toolApproval,
        IConfiguration configuration,
        ILogger<GoogleLLMService> logger)
    {
        _kernelFactory = kernelFactory;
        _eCommerceApi = eCommerceApi;
        _toolApproval = toolApproval;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LLMResponse> GenerateAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId) || !Guid.TryParse(request.SessionId, out _))
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
        var kernel = _kernelFactory.CreateKernel(_eCommerceApi, request.SessionId);
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var settings = CreatePromptExecutionSettings(request);

        IReadOnlyList<ChatMessageContent> contents;
        try
        {
            // TODO: o conector Google (Microsoft.SemanticKernel.Connectors.Google 1.74.0-alpha)
            // ainda não suporta de forma estável InvokePromptStreamingAsync com auto-invocação
            // de funções. Usamos GetChatMessageContentsAsync como fallback, mantendo o mesmo
            // contrato do OpenAILLMService. Reavaliar quando o conector sair de alpha.
            contents = await chat
                .GetChatMessageContentsAsync(history, settings, kernel, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no chat completion do Google/Gemini/SK.");
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
            _logger.LogWarning("A API Google/Gemini devolveu resposta vazia após a invocação.");
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

#pragma warning disable SKEXP0070
    private GeminiPromptExecutionSettings CreatePromptExecutionSettings(LLMRequest request)
    {
        var temperature = request.Temperature > 0
            ? request.Temperature
            : GetConfiguredTemperature();

        var maxTokens = request.MaxTokens > 0
            ? request.MaxTokens
            : GetConfiguredMaxOutputTokens();

        return new GeminiPromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
            Temperature = temperature,
            MaxTokens = maxTokens
        };
    }
#pragma warning restore SKEXP0070

    private double GetConfiguredTemperature()
    {
        if (double.TryParse(
                _configuration["LLM:Google:Temperature"],
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var t) && t >= 0)
        {
            return t;
        }
        return DefaultTemperature;
    }

    private int GetConfiguredMaxOutputTokens()
    {
        if (int.TryParse(_configuration["LLM:Google:MaxOutputTokens"], out var n) && n > 0)
        {
            return n;
        }
        return DefaultMaxOutputTokens;
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
