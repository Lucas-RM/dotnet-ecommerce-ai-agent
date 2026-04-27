using System.Diagnostics;
using System.Net;
using System.Text.Json;
using ECommerce.AgentAPI.Application.Capabilities;
using ECommerce.AgentAPI.Domain.Entities;
using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.LLM;
using ECommerce.AgentAPI.Application.Abstractions;
using Microsoft.SemanticKernel;
using Refit;

namespace ECommerce.AgentAPI.Infrastructure.Tools;

public sealed class ToolExecutorService : IToolExecutor
{
    private readonly IKernelFactory _kernelFactory;
    private readonly IECommerceApi _eCommerceApi;
    private readonly ToolApprovalService _toolApproval;
    private readonly IAgentObservability _observability;

    public ToolExecutorService(
        IKernelFactory kernelFactory,
        IECommerceApi eCommerceApi,
        ToolApprovalService toolApproval,
        IAgentObservability observability)
    {
        _kernelFactory = kernelFactory;
        _eCommerceApi = eCommerceApi;
        _toolApproval = toolApproval;
        _observability = observability;
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

        var name = toolCall.Name ?? string.Empty;
        var cap = ToolCapabilityResolver.Resolve(name);
        var sw = Stopwatch.StartNew();
        using var toolActivity = _observability.StartToolActivity(
            name,
            toolCall.SessionId,
            toolCall.CorrelationId);
        try
        {
            var kernel = _kernelFactory.CreateKernel(_eCommerceApi, toolCall.SessionId);
            if (_toolApproval.RequiresApproval(name))
            {
                _toolApproval.PrepareApprovedExecution(kernel);
            }

            var fn = ResolveKernelFunction(kernel, name);
            var args = ToKernelArguments(toolCall.Arguments);
            var result = await fn
                .InvokeAsync(kernel, args, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var text = result.GetValue<string>() ?? result.ToString() ?? string.Empty;
            var (success, data, envelopeError) = PluginEnvelopeUnwrapper.Unwrap(text);
            sw.Stop();
            _observability.RecordToolDuration(
                name,
                cap,
                sw.Elapsed,
                success,
                !success
                    ? "plugin_envelope"
                    : null);
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
            return FailWithObs(
                sw,
                name,
                cap,
                "not_found",
                "Não foi possível executar a ação solicitada. Tente de novo em instantes.");
        }
        catch (NotSupportedException)
        {
            return FailWithObs(
                sw,
                name,
                cap,
                "llm_unavailable",
                "Serviço de IA indisponível. Tente de novo em instantes.");
        }
        catch (ArgumentException)
        {
            return FailWithObs(
                sw,
                name,
                cap,
                "session_invalid",
                "Sessão inválida.");
        }
        catch (Exception ex)
        {
            var code = ex is ApiException a ? "api_" + (int)a.StatusCode : "internal";
            return FailWithObs(
                sw,
                name,
                cap,
                code,
                FormatECommerceToolError(ex));
        }
    }

    private ToolExecutionResult FailWithObs(
        Stopwatch sw,
        string name,
        AgentCapability cap,
        string errorKind,
        string userMessage)
    {
        if (sw.IsRunning)
            sw.Stop();
        _observability.RecordToolDuration(name, cap, sw.Elapsed, false, errorKind);
        return FailResult(name, userMessage);
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
