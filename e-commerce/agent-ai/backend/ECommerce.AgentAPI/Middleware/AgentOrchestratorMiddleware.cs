using System.Net;
using System.Text.RegularExpressions;
using ECommerce.AgentAPI.Approval;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Kernel;
using ECommerce.AgentAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ECommerce.AgentAPI.Middleware;

/// <summary>
/// Pipeline de orquestração: aprovação pendente (confirmar / negar / ambíguo) ou delegação ao Semantic Kernel.
/// </summary>
public sealed class AgentOrchestratorMiddleware
{
    private static readonly Regex WordBoundarySim = new(@"\b(sim|sí|ok|okay|pode|confirma|confirmo|aceito|claro|dale|fechado|manda|isso)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex WordBoundaryNao = new(@"\b(não|nao|negativo|cancela|cancelar)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly KernelFactory _kernelFactory;
    private readonly ToolApprovalService _toolApproval;
    private readonly AgentMemoryStore _memoryStore;
    private readonly IECommerceApi _ecommerceApi;
    private readonly ILogger<AgentOrchestratorMiddleware> _logger;

    public AgentOrchestratorMiddleware(
        KernelFactory kernelFactory,
        ToolApprovalService toolApproval,
        AgentMemoryStore memoryStore,
        IECommerceApi ecommerceApi,
        ILogger<AgentOrchestratorMiddleware> logger)
    {
        _kernelFactory = kernelFactory;
        _toolApproval = toolApproval;
        _memoryStore = memoryStore;
        _ecommerceApi = ecommerceApi;
        _logger = logger;
    }

    public async Task<ChatProcessResult> ProcessAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var sessionKey = request.SessionId.ToString("D");

        if (_toolApproval.HasPending(sessionKey))
            return Ok(await HandlePendingApprovalAsync(request, sessionKey, cancellationToken).ConfigureAwait(false));

        return await InvokeKernelChatAsync(request, sessionKey, cancellationToken).ConfigureAwait(false);
    }

    private static ChatProcessResult Ok(ChatResponse response) =>
        new(StatusCodes.Status200OK, response);

    private async Task<ChatResponse> HandlePendingApprovalAsync(
        ChatRequest request,
        string sessionKey,
        CancellationToken cancellationToken)
    {
        if (!_toolApproval.TryGetPending(sessionKey, out var pendingRef) || pendingRef is null)
        {
            _logger.LogWarning("HasPending era true mas TryGetPending falhou para a sessão {Session}", sessionKey);
            return new ChatResponse { Reply = "Não foi possível recuperar a ação pendente. Envie sua mensagem novamente.", RequiresApproval = false };
        }

        var intent = ClassifyApprovalIntent(request.Message);

        switch (intent)
        {
            case ApprovalIntent.Denied:
                _toolApproval.ClearPending(sessionKey);
                var denyReply = "Ok, cancelei. Posso ajudar com mais alguma coisa?";
                AppendTurn(sessionKey, request.Message, denyReply);
                return new ChatResponse { Reply = denyReply, RequiresApproval = false };

            case ApprovalIntent.Ambiguous:
                var clarify =
                    "Preciso de uma confirmação objetiva: responda **sim** para prosseguir ou **não** para cancelar.";
                AppendTurn(sessionKey, request.Message, clarify);
                return new ChatResponse
                {
                    Reply = clarify,
                    RequiresApproval = true,
                    PendingToolName = pendingRef.FunctionName
                };

            case ApprovalIntent.Confirmed:
            default:
                if (!_toolApproval.TryRemovePending(sessionKey, out var pending) || pending is null)
                {
                    return new ChatResponse { Reply = "Não há mais ação pendente para confirmar.", RequiresApproval = false };
                }

                var kernel = _kernelFactory.CreateKernel(_ecommerceApi, request.SessionId);
                _toolApproval.PrepareApprovedExecution(kernel);

                KernelFunction function;
                try
                {
                    function = kernel.Plugins.GetFunction(pending.PluginName, pending.FunctionName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Função pendente não encontrada no kernel: {Plugin}.{Function}", pending.PluginName, pending.FunctionName);
                    return new ChatResponse
                    {
                        Reply = "Não foi possível executar a ação confirmada. Tente novamente mais tarde.",
                        RequiresApproval = false
                    };
                }

                string toolOutput;
                try
                {
                    var invokeResult = await function
                        .InvokeAsync(kernel, pending.Arguments, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                    toolOutput = invokeResult.GetValue<string>()
                        ?? invokeResult.ToString()
                        ?? string.Empty;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao executar tool aprovada {Function}", pending.FunctionName);
                    return new ChatResponse
                    {
                        Reply = "Ocorreu um erro ao executar a ação confirmada. Tente novamente.",
                        RequiresApproval = false
                    };
                }

                AppendTurn(sessionKey, request.Message, toolOutput);
                return new ChatResponse { Reply = toolOutput, RequiresApproval = false };
        }
    }

    private void AppendTurn(string sessionKey, string userMessage, string assistantReply)
    {
        var history = _memoryStore.GetOrCreate(sessionKey);
        EnsureSystemMessage(history);
        history.AddUserMessage(userMessage);
        history.AddAssistantMessage(assistantReply);
        _memoryStore.TrimConversation(sessionKey);
    }

    private async Task<ChatProcessResult> InvokeKernelChatAsync(
        ChatRequest request,
        string sessionKey,
        CancellationToken cancellationToken)
    {
        var history = _memoryStore.GetOrCreate(sessionKey);
        EnsureSystemMessage(history);
        history.AddUserMessage(request.Message);

        var kernel = _kernelFactory.CreateKernel(_ecommerceApi, request.SessionId);
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var settings = _kernelFactory.CreatePromptExecutionSettings();

        IReadOnlyList<ChatMessageContent> contents;
        try
        {
            contents = await chat
                .GetChatMessageContentsAsync(history, settings, kernel, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha no chat completion do Semantic Kernel.");
            history.RemoveAt(history.Count - 1);
            var (status, errorReply) = MapKernelFailure(ex);
            return new ChatProcessResult(status, new ChatResponse { Reply = errorReply, RequiresApproval = false });
        }

        _memoryStore.TrimConversation(sessionKey);

        var assistantReply = ExtractAssistantReply(contents);

        if (_toolApproval.TryGetPending(sessionKey, out var pending) && pending is not null)
        {
            return Ok(new ChatResponse
            {
                Reply = string.IsNullOrWhiteSpace(assistantReply) ? pending.ApprovalMessage : assistantReply,
                RequiresApproval = true,
                PendingToolName = pending.FunctionName
            });
        }

        return Ok(new ChatResponse { Reply = assistantReply, RequiresApproval = false });
    }

    /// <summary>
    /// Falhas da OpenAI chegam como <see cref="HttpOperationException"/> (ex.: 429 quota, 429 rate limit).
    /// </summary>
    private static (int StatusCode, string Reply) MapKernelFailure(Exception ex)
    {
        if (ex is HttpOperationException httpOp)
        {
            var code = (int)(httpOp.StatusCode ?? HttpStatusCode.BadGateway);

            if (code == (int)HttpStatusCode.TooManyRequests || IsInsufficientQuota(ex))
            {
                var msg = IsInsufficientQuota(ex)
                    ? "O serviço de IA não está disponível: cota ou faturamento da API OpenAI esgotado. Confira plano e saldo em https://platform.openai.com/account/billing"
                    : "Limite de requisições da API de IA atingido. Tente novamente em alguns instantes.";
                return (StatusCodes.Status429TooManyRequests, msg);
            }

            if (code >= 500)
                return (StatusCodes.Status502BadGateway,
                    "O provedor de IA retornou erro temporário. Tente novamente em instantes.");

            return (StatusCodes.Status502BadGateway,
                "Não foi possível concluir a chamada ao modelo de IA. Verifique a chave e as configurações em appsettings.");
        }

        return (StatusCodes.Status503ServiceUnavailable,
            "Desculpe, não consegui processar sua mensagem agora. Tente novamente em instantes.");
    }

    private static bool IsInsufficientQuota(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            var msg = e.Message ?? string.Empty;
            if (msg.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void EnsureSystemMessage(ChatHistory history)
    {
        if (history.Any(m => m.Role == AuthorRole.System))
            return;
        history.Insert(0, new ChatMessageContent(AuthorRole.System, AgentSystemPrompt.Text));
    }

    private static string ExtractAssistantReply(IReadOnlyList<ChatMessageContent> contents)
    {
        for (var i = contents.Count - 1; i >= 0; i--)
        {
            if (contents[i].Role == AuthorRole.Assistant)
                return contents[i].Content ?? string.Empty;
        }

        return string.Empty;
    }

    private static ApprovalIntent ClassifyApprovalIntent(string message)
    {
        var t = message.Trim();
        if (string.IsNullOrEmpty(t))
            return ApprovalIntent.Ambiguous;

        if (t.Equals("s", StringComparison.OrdinalIgnoreCase))
            return ApprovalIntent.Confirmed;

        if (WordBoundaryNao.IsMatch(t))
            return ApprovalIntent.Denied;

        if (t.Contains("deixa quieto", StringComparison.OrdinalIgnoreCase)
            || t.Contains("esquece", StringComparison.OrdinalIgnoreCase)
            || t.Contains("melhor não", StringComparison.OrdinalIgnoreCase))
            return ApprovalIntent.Denied;

        if (WordBoundarySim.IsMatch(t))
            return ApprovalIntent.Confirmed;

        return ApprovalIntent.Ambiguous;
    }

    private enum ApprovalIntent
    {
        Confirmed,
        Denied,
        Ambiguous
    }
}
