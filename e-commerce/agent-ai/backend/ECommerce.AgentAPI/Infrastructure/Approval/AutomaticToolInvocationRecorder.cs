using ECommerce.AgentAPI.Domain.ValueObjects;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.Approval;

/// <summary>Grava o resultado de cada <c>KernelFunction</c> concluída (após <c>next</c>) no <see cref="Microsoft.SemanticKernel.Kernel.Data" />.
/// Só entra no fluxo de gravação quando a real execução do plugin ocorreu (não no ramo de interceção de aprovação).</summary>
public static class AutomaticToolInvocationRecorder
{
    public static void AppendIfPossible(FunctionInvocationContext context)
    {
        if (!context.Kernel.Data.TryGetValue(AgentKernelDataKeys.AutomaticToolInvocations, out var raw)
            || raw is not List<RecordedToolInvocation> list)
        {
            return;
        }

        var s = GetRawResultString(context);
        if (string.IsNullOrWhiteSpace(s))
        {
            return;
        }

        list.Add(
            new RecordedToolInvocation
            {
                FunctionName = context.Function.Name,
                RawResult = s
            });
    }

    private static string? GetRawResultString(FunctionInvocationContext context)
    {
        if (context.Result is FunctionResult fr)
        {
            return fr.GetValue<string>() ?? fr.ToString();
        }

        return context.Result?.ToString();
    }
}
