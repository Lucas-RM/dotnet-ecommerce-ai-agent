using System.Net;
using System.Text.Json;
using ECommerce.AgentAPI.Domain.Entities;
using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.LLM;
using Microsoft.SemanticKernel;
using Refit;

namespace ECommerce.AgentAPI.Infrastructure.Tools;

public sealed class ToolExecutorService : IToolExecutor
{
    private readonly IKernelFactory _kernelFactory;
    private readonly IECommerceApi _eCommerceApi;
    private readonly ToolApprovalService _toolApproval;

    public ToolExecutorService(
        IKernelFactory kernelFactory,
        IECommerceApi eCommerceApi,
        ToolApprovalService toolApproval)
    {
        _kernelFactory = kernelFactory;
        _eCommerceApi = eCommerceApi;
        _toolApproval = toolApproval;
    }

    public async Task<ToolExecutionResult> ExecuteAsync(
        ToolCall toolCall,
        string jwtToken,
        CancellationToken cancellationToken = default)
    {
        _ = jwtToken;
        if (string.IsNullOrWhiteSpace(toolCall.SessionId))
            return FailResult(toolCall.Name, "Sessão inválida.");

        if (string.IsNullOrWhiteSpace(toolCall.Name))
            return FailResult(null, "Tool sem nome.");

        try
        {
            var kernel = _kernelFactory.CreateKernel(_eCommerceApi, toolCall.SessionId);
            if (_toolApproval.RequiresApproval(toolCall.Name))
            {
                _toolApproval.PrepareApprovedExecution(kernel);
            }

            var fn = ResolveKernelFunction(kernel, toolCall.Name);
            var args = ToKernelArguments(toolCall.Arguments);
            var result = await fn
                .InvokeAsync(kernel, args, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var text = result.GetValue<string>() ?? result.ToString() ?? string.Empty;
            var (success, data, envelopeError) = UnwrapEnvelope(text);
            return new ToolExecutionResult
            {
                Success = success,
                Output = text,
                Error = envelopeError,
                ToolName = toolCall.Name,
                Data = data
            };
        }
        catch (KeyNotFoundException)
        {
            return FailResult(toolCall.Name, "Não foi possível executar a ação solicitada. Tente de novo em instantes.");
        }
        catch (NotSupportedException)
        {
            return FailResult(toolCall.Name, "Serviço de IA indisponível. Tente de novo em instantes.");
        }
        catch (ArgumentException)
        {
            return FailResult(toolCall.Name, "Sessão inválida.");
        }
        catch (Exception ex)
        {
            return FailResult(toolCall.Name, FormatECommerceToolError(ex));
        }
    }

    private static ToolExecutionResult FailResult(string? toolName, string error) =>
        new()
        {
            Success = false,
            Output = string.Empty,
            Error = error,
            ToolName = toolName,
            Data = null
        };

    /// <summary>
    /// Desembrulha o envelope JSON devolvido pelos plugins. Aceita três formatos:
    /// 1. Envelope padrão <c>ECommerceApiResponse{T}</c> → <c>{ success, data, message, errors }</c>;
    /// 2. Envelope ad-hoc dos plugins para erros locais → <c>{ success: false, message }</c>;
    /// 3. JSON cru (sem envelope) → devolvido como está em <c>Data</c>.
    /// Para não JSON (texto livre), devolve <c>Data = null</c> e <c>Success = true</c>.
    /// </summary>
    private static (bool Success, JsonElement? Data, string? Error) UnwrapEnvelope(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return (true, null, null);
        }

        JsonDocument? doc = null;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return (true, null, null);
        }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return (true, root.Clone(), null);
            }

            var hasSuccess = root.TryGetProperty("success", out var successEl)
                             && successEl.ValueKind is JsonValueKind.True or JsonValueKind.False;
            if (!hasSuccess)
            {
                return (true, root.Clone(), null);
            }

            var success = successEl.GetBoolean();
            string? error = null;

            if (!success)
            {
                if (root.TryGetProperty("message", out var msgEl) && msgEl.ValueKind == JsonValueKind.String)
                {
                    error = msgEl.GetString();
                }

                if (string.IsNullOrWhiteSpace(error)
                    && root.TryGetProperty("errors", out var errsEl)
                    && errsEl.ValueKind == JsonValueKind.Array)
                {
                    var first = errsEl.EnumerateArray()
                        .FirstOrDefault(e => e.ValueKind == JsonValueKind.String);
                    if (first.ValueKind == JsonValueKind.String)
                    {
                        error = first.GetString();
                    }
                }
            }

            JsonElement? data = null;
            if (root.TryGetProperty("data", out var dataEl) && dataEl.ValueKind != JsonValueKind.Null)
            {
                data = dataEl.Clone();
            }

            return (success, data, error);
        }
    }

    private static string FormatECommerceToolError(Exception ex)
    {
        for (var it = ex; it is not null; it = it.InnerException)
        {
            if (it is ApiException api)
            {
                var body = ECommerceApiErrorMessageReader.TryGetMessageFromApiException(api);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    return body;
                }

                return MapApiExceptionToUserMessage(api);
            }
        }

        return ex.Message;
    }

    private static string MapApiExceptionToUserMessage(ApiException api) =>
        api.StatusCode switch
        {
            HttpStatusCode.BadRequest =>
                "Não foi possível concluir a operação: o pedido foi recusado pela loja. Tente buscar o produto de novo ou confirme o item na lista.",
            HttpStatusCode.NotFound => "Não encontrei esse recurso na loja.",
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                "Não foi possível aceder à loja. Inicie sessão de novo se o problema continuar.",
            _ => "Não foi possível concluir a operação na loja. Tente de novo em instantes."
        };

    private static KernelArguments ToKernelArguments(Dictionary<string, object> map)
    {
        var a = new KernelArguments();
        foreach (var kv in map)
        {
            a[kv.Key] = kv.Value;
        }
        return a;
    }

    /// <summary>
    /// Localiza a <see cref="KernelFunction"/> pelo nome anotado em <c>[KernelFunction]</c>,
    /// varrendo todos os plugins registados no kernel. Evita o mapeamento manual
    /// <c>functionName → pluginName</c>: o próprio SK já conhece essa relação.
    /// </summary>
    private static KernelFunction ResolveKernelFunction(Microsoft.SemanticKernel.Kernel kernel, string functionName)
    {
        foreach (var plugin in kernel.Plugins)
        {
            if (plugin.TryGetFunction(functionName, out var fn))
                return fn;
        }

        throw new KeyNotFoundException($"Tool '{functionName}' não está registrada em nenhum plugin do kernel.");
    }
}
