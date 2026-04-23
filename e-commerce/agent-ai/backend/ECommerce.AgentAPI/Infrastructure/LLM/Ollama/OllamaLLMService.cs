using ECommerce.AgentAPI.Domain.Entities;
using ECommerce.AgentAPI.Domain.Enums;
using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Approval;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SkChat = Microsoft.SemanticKernel.ChatCompletion;
using System.Net.Sockets;

namespace ECommerce.AgentAPI.Infrastructure.LLM.Ollama;

public sealed class OllamaLLMService : ILLMService
{
    private const string EmptyReplyMessage =
        "Não obtive uma resposta utilizável do modelo. Verifique o Ollama e o modelo (LLM:Ollama:Model) e tente de novo.";

    private readonly OllamaKernelFactory _kernelFactory;
    private readonly IECommerceApi _eCommerceApi;
    private readonly ToolApprovalService _toolApproval;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OllamaLLMService> _logger;

    public OllamaLLMService(
        OllamaKernelFactory kernelFactory,
        IECommerceApi eCommerceApi,
        ToolApprovalService toolApproval,
        IConfiguration configuration,
        ILogger<OllamaLLMService> logger)
    {
        _kernelFactory = kernelFactory;
        _eCommerceApi = eCommerceApi;
        _toolApproval = toolApproval;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LLMResponse> GenerateAsync(
        LLMRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SessionId) || !Guid.TryParse(request.SessionId, out var sessionGuid))
        {
            return new LLMResponse
            {
                Text = "Sess?o inv?lida para o motor de IA.",
                HasToolCall = false,
                FinishReason = "error"
            };
        }

        var key = request.SessionId;
        var history = ToSkChatHistory(request);
        var kernel = _kernelFactory.CreateKernel(_eCommerceApi, sessionGuid);
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var settings = _kernelFactory.CreatePromptExecutionSettings();
        if (request.Temperature > 0)
        {
            settings.Temperature = (float)request.Temperature;
        }

        var maxOut = GetConfiguredMaxOutputTokens();
        if (request.MaxTokens > 0)
        {
            settings.NumPredict = Math.Min(request.MaxTokens, maxOut);
        }
        else
        {
            settings.NumPredict = maxOut;
        }

        if (int.TryParse(_configuration["LLM:Ollama:TopK"], out var topK) && topK > 0)
        {
            settings.TopK = topK;
        }
        else
        {
            settings.TopK = 25;
        }

        if (float.TryParse(
                _configuration["LLM:Ollama:TopP"],
                System.Globalization.CultureInfo.InvariantCulture,
                out var topP) && topP is > 0 and <= 1)
        {
            settings.TopP = topP;
        }
        else
        {
            settings.TopP = 0.5f;
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
            if (LooksLikeOllamaConnectivityIssue(ex))
            {
                throw new HttpRequestException(
                    "Falha de conectividade com Ollama. Verifique se o servi?o est? ativo e acess?vel em LLM:Ollama:BaseUrl.",
                    ex);
            }

            _logger.LogError(ex, "Falha no chat completion do Ollama/SK.");
            throw;
        }

        if (_toolApproval.TryGetPending(key, out var p) && p is not null)
        {
            return new LLMResponse
            {
                Text = null,
                HasToolCall = true,
                ToolCall = ToToolCall(p, key),
                FinishReason = "tool_calls"
            };
        }

        var text = ExtractAssistantReply(contents);
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("O Ollama devolveu conte?do vazio ap?s a invoca??o (sem aprova??o pendente).");
            text = EmptyReplyMessage;
        }

        return new LLMResponse
        {
            Text = text,
            HasToolCall = false,
            FinishReason = "stop"
        };
    }

    private int GetConfiguredMaxOutputTokens()
    {
        if (int.TryParse(_configuration["LLM:Ollama:MaxOutputTokens"], out var n) && n > 0)
            return n;
        return 512;
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

    private static bool LooksLikeOllamaConnectivityIssue(Exception exception)
    {
        for (var it = exception; it is not null; it = it.InnerException)
        {
            if (it is HttpRequestException or SocketException or TimeoutException)
            {
                return true;
            }

            var msg = it.Message ?? string.Empty;
            if (msg.Contains("localhost:11434", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("connection refused", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("actively refused", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}