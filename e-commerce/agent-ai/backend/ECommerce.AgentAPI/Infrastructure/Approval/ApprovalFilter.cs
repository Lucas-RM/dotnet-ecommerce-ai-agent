using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.Approval;

/// <summary>
/// <see cref="IFunctionInvocationFilter"/> que intercetia invocações a tools configuradas
/// (via <see cref="ToolApprovalService.RequiresApproval"/>): se não houver
/// <see cref="ToolApprovalService.PrepareApprovedExecution"/>, a execução é bloqueada, grava
/// <see cref="PendingToolCall"/> e devolve a mensagem de aprovação. Se o orquestrador
/// tiver sinalizado <c>Agent.SkipApprovalOnce</c> no <see cref="Kernel"/>, a invocação segue
/// (uma única vez) e a flag é removida.
/// </summary>
public sealed class ApprovalFilter(ToolApprovalService toolApproval) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var functionName = context.Function.Name;
        if (!toolApproval.RequiresApproval(functionName))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        if (TryConsumeSkipApprovalOnce(context.Kernel))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var sessionId = ResolveSessionId(context.Kernel);
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            context.Result = new FunctionResult(
                context.Function,
                "Erro interno: sessão do agent não encontrada no Kernel; não foi possível solicitar aprovação.");
            return;
        }

        var message = toolApproval.BuildApprovalMessage(functionName, context.Arguments);
        var pending = toolApproval.CreatePendingFromInvocation(context.Function, context.Arguments, message);
        toolApproval.StorePending(sessionId, pending);

        context.Result = new FunctionResult(context.Function, message);
    }

    private static bool TryConsumeSkipApprovalOnce(Microsoft.SemanticKernel.Kernel kernel)
    {
        if (!kernel.Data.TryGetValue(AgentKernelDataKeys.SkipApprovalOnce, out var raw) || raw is not bool b || !b)
            return false;
        kernel.Data.Remove(AgentKernelDataKeys.SkipApprovalOnce);
        return true;
    }

    private static string? ResolveSessionId(Microsoft.SemanticKernel.Kernel kernel)
    {
        if (!kernel.Data.TryGetValue(AgentKernelDataKeys.SessionId, out var v) || v is null)
            return null;
        return v switch
        {
            Guid g => g.ToString("D"),
            string s => s,
            _ => v.ToString()
        };
    }
}
