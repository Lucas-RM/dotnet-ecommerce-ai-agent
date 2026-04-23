using System.Net;
using ECommerce.AgentAPI.Domain.Entities;
using ECommerce.AgentAPI.Domain.Interfaces;
using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.ECommerceClient;
using ECommerce.AgentAPI.Infrastructure.Approval;
using ECommerce.AgentAPI.Infrastructure.LLM.OpenAI;
using Microsoft.SemanticKernel;
using Refit;

namespace ECommerce.AgentAPI.Infrastructure.Tools;

public sealed class ToolExecutorService : IToolExecutor
{
    private readonly KernelFactory _kernelFactory;
    private readonly IECommerceApi _eCommerceApi;
    private readonly ToolApprovalService _toolApproval;

    public ToolExecutorService(
        KernelFactory kernelFactory,
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
        if (string.IsNullOrWhiteSpace(toolCall.SessionId) || !Guid.TryParse(toolCall.SessionId, out var sessionId))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Output = string.Empty,
                Error = "Sessão inválida."
            };
        }

        if (string.IsNullOrWhiteSpace(toolCall.Name))
        {
            return new ToolExecutionResult
            {
                Success = false,
                Output = string.Empty,
                Error = "Tool sem nome."
            };
        }

        try
        {
            var kernel = _kernelFactory.CreateKernel(_eCommerceApi, sessionId);
            if (_toolApproval.RequiresApproval(toolCall.Name))
            {
                _toolApproval.PrepareApprovedExecution(kernel);
            }

            var plugin = ToolApprovalServiceAdapter.ResolvePluginName(toolCall.Name);
            var function = toolCall.Name;
            var fn = kernel.Plugins.GetFunction(plugin, function);
            var args = ToKernelArguments(toolCall.Arguments);
            var result = await fn
                .InvokeAsync(kernel, args, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var text = result.GetValue<string>() ?? result.ToString() ?? string.Empty;
            return new ToolExecutionResult { Success = true, Output = text, Error = null };
        }
        catch (Exception ex)
        {
            return new ToolExecutionResult
            {
                Success = false,
                Output = string.Empty,
                Error = FormatECommerceToolError(ex)
            };
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
}
