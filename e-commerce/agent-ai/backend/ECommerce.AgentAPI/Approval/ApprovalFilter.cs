using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Approval;

/// <summary>
/// Intercepta invocações de <see cref="KernelFunction"/> que exigem aprovação: armazena a call pendente e
/// devolve a mensagem de confirmação sem executar o plugin (<c>next</c> não é chamado).
/// </summary>
public sealed class ApprovalFilter(ToolApprovalService toolApproval) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        if (TryConsumeSkipApprovalOnce(context.Kernel))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var functionName = context.Function.Name;
        if (!toolApproval.RequiresApproval(functionName))
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
        // Não chama next — a tool não roda até confirmação (middleware usará PrepareApprovedExecution + nova invocação).
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
            Guid g => g.ToString(),
            string s => s,
            _ => v.ToString()
        };
    }
}
