using System.IO;
using System.Net;
using ECommerce.AgentAPI.Application.Abstractions;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Refit;

namespace ECommerce.AgentAPI.Infrastructure.ErrorHandling;

public sealed class HttpChatErrorHandler : IChatErrorHandler
{
    private readonly ILogger<HttpChatErrorHandler> _logger;

    public HttpChatErrorHandler(ILogger<HttpChatErrorHandler> logger) =>
        _logger = logger;

    public ChatProcessResult MapToProcessResult(Exception exception)
    {
        var ex = UnwrapChainedExceptions(exception);
        (int sc, var reply) = ex switch
        {
            ApiException refit => MapByHttpCode((int)refit.StatusCode, fromECommerceApi: true, refit),
            HttpRequestException _ => (StatusCodes.Status503ServiceUnavailable,
                "Não foi possível ligar ao serviço remoto. Verifique a ligação de rede e tente novamente em instantes."),
            OperationCanceledException _ => (StatusCodes.Status503ServiceUnavailable,
                "O pedido demorou demais e foi interrompido. Tente de novo em instantes."),
            HttpOperationException httpOp => MapByHttpCode((int)(httpOp.StatusCode ?? HttpStatusCode.BadGateway), fromECommerceApi: false, httpOp),
            System.Net.Sockets.SocketException
                or IOException
                or TimeoutException _ => (StatusCodes.Status503ServiceUnavailable,
                "Problema de ligação de rede. Tente de novo em instantes."),
            KernelException kex when LooksLikeOllamaConnectorIssue(kex) => (StatusCodes.Status503ServiceUnavailable,
                "Não foi possível concluir a chamada ao Ollama. Verifique se o Ollama está ativo, se o modelo está instalado e se LLM:Ollama:BaseUrl/Model estão corretos."),
            KernelException => (StatusCodes.Status503ServiceUnavailable,
                "Falha no motor de conversação (Semantic Kernel). Verifique o modelo, ferramentas suportadas e a configuração em LLM. Os detalhes técnicos foram registados nos logs do servidor."),
            _ => UnmappedError(exception)
        };
        return new ChatProcessResult(sc, new ChatResponse { Reply = reply, RequiresApproval = false });
    }

    private (int StatusCode, string Reply) UnmappedError(Exception original)
    {
        _logger.LogError(original, "Exceção não mapeada ao processar a mensagem do agente; ver o trace para a causa.");
        return (StatusCodes.Status503ServiceUnavailable,
            "Desculpe, não consegui processar sua mensagem agora. Tente novamente em instantes.");
    }

    private static (int StatusCode, string Reply) MapByHttpCode(int code, bool fromECommerceApi, Exception? e = null)
    {
        var eCommerceMsg = ECommerceDetail(e, fromECommerceApi);
        if (code == (int)HttpStatusCode.TooManyRequests)
        {
            var isQuota = e is not null && IsInsufficientQuota(e);
            if (isQuota)
            {
                return (StatusCodes.Status429TooManyRequests,
                    "O serviço de IA não está disponível: cota ou faturamento da API OpenAI esgotado. Confira plano e saldo em https://platform.openai.com/account/billing");
            }

            // 429 a montante da API da loja → 502 (rate limit do upstream, não do cliente)
            if (fromECommerceApi)
            {
                return (StatusCodes.Status502BadGateway,
                    eCommerceMsg
                    ?? "O serviço da loja está a limitar requisições. Tente de novo em alguns instantes.");
            }

            return (StatusCodes.Status429TooManyRequests,
                "Limite de requisições da API de IA atingido. Tente novamente em alguns instantes.");
        }

        if (code == (int)HttpStatusCode.BadGateway)
        {
            if (fromECommerceApi)
            {
                return (StatusCodes.Status502BadGateway,
                    eCommerceMsg
                    ?? "A loja respondeu de forma inesperada (gateway). Tente novamente em instantes.");
            }

            return (StatusCodes.Status502BadGateway,
                "A API de IA respondeu de forma inesperada. Tente novamente em instantes.");
        }

        if (code == (int)HttpStatusCode.ServiceUnavailable)
        {
            if (fromECommerceApi)
            {
                return (StatusCodes.Status503ServiceUnavailable,
                    eCommerceMsg
                    ?? "O serviço da loja está temporariamente indisponível. Tente em instantes.");
            }

            return (StatusCodes.Status503ServiceUnavailable,
                "O serviço de IA está temporariamente indisponível. Tente de novo em instantes.");
        }

        if (code is >= 500 and < 600)
        {
            if (fromECommerceApi)
            {
                return (StatusCodes.Status502BadGateway,
                    eCommerceMsg
                    ?? "O serviço da loja apresentou um erro temporário. Tente de novo em instantes.");
            }

            return (StatusCodes.Status502BadGateway,
                "O serviço de IA apresentou um erro temporário. Tente de novo em instantes.");
        }

        if (code is >= 400 and < 500)
        {
            if (e is { } e2 && IsInsufficientQuota(e2))
            {
                return (StatusCodes.Status429TooManyRequests,
                    "O serviço de IA não está disponível: cota ou faturamento da API OpenAI esgotado. Confira plano e saldo em https://platform.openai.com/account/billing");
            }

            if (fromECommerceApi)
            {
                return (StatusCodes.Status502BadGateway,
                    eCommerceMsg
                    ?? "Não foi possível concluir a operação no serviço da loja. Tente de novo em instantes.");
            }

            return (StatusCodes.Status502BadGateway,
                "Não foi possível concluir a chamada ao modelo de IA. Verifique a chave e as definições em appsettings.");
        }

        if (fromECommerceApi)
        {
            return (StatusCodes.Status502BadGateway,
                eCommerceMsg ?? "Não foi possível concluir a operação no serviço da loja.");
        }

        return (StatusCodes.Status502BadGateway,
            "Não foi possível concluir a chamada ao modelo de IA. Verifique a chave e as definições em appsettings.");
    }

    private static string? ECommerceDetail(Exception? e, bool fromECommerceApi) =>
        fromECommerceApi && e is ApiException api
            ? ECommerceApiErrorMessageReader.TryGetMessageFromApiException(api)
            : null;

    private static Exception UnwrapChainedExceptions(Exception e)
    {
        if (e is AggregateException a && a.InnerExceptions.Count is 1)
            return UnwrapChainedExceptions(a.InnerExceptions[0]);
        if (e is AggregateException ag)
        {
            foreach (var inner in ag.InnerExceptions)
            {
                var d = UnwrapChainedExceptions(inner);
                if (d is ApiException
                    or HttpOperationException
                    or HttpRequestException
                    or OperationCanceledException
                    or TimeoutException
                    or System.Net.Sockets.SocketException
                    or IOException
                    or KernelException)
                    return d;
            }
        }

        for (var it = e; it is not null; it = it.InnerException)
        {
            if (it is HttpOperationException)
                return it;
        }

        for (var it = e; it is not null; it = it.InnerException)
        {
            if (it is ApiException)
                return it;
        }

        for (var it = e; it is not null; it = it.InnerException)
        {
            if (it is HttpRequestException)
                return it;
        }

        for (var it = e; it is not null; it = it.InnerException)
        {
            if (it is System.Net.Sockets.SocketException or TimeoutException or IOException)
                return it;
        }

        for (var it = e; it is not null; it = it.InnerException)
        {
            if (it is KernelException)
                return it;
        }

        return e;
    }

    private static bool IsInsufficientQuota(Exception e)
    {
        for (var it = e; it is not null; it = it.InnerException)
        {
            var msg = it.Message ?? string.Empty;
            if (msg.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool LooksLikeOllamaConnectorIssue(Exception exception)
    {
        for (var it = exception; it is not null; it = it.InnerException)
        {
            var msg = it.Message ?? string.Empty;
            if (msg.Contains("ollama", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("11434", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("connection refused", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("actively refused", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("function", StringComparison.OrdinalIgnoreCase)
                || msg.Contains("tool", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
