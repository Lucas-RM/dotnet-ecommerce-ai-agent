using ECommerce.AgentAPI.Domain.ValueObjects;
using ECommerce.AgentAPI.Infrastructure.Approval;
using Microsoft.SemanticKernel;

namespace ECommerce.AgentAPI.Infrastructure.LLM;

/// <summary>Cópia de leitura da lista de invocações gravada em <see cref="AgentKernelDataKeys.AutomaticToolInvocations" />.</summary>
internal static class KernelAutomaticToolListReader
{
    public static IReadOnlyList<RecordedToolInvocation> ReadFromKernel(Microsoft.SemanticKernel.Kernel kernel)
    {
        if (!kernel.Data.TryGetValue(AgentKernelDataKeys.AutomaticToolInvocations, out var raw)
            || raw is not List<RecordedToolInvocation> list
            || list.Count == 0)
        {
            return Array.Empty<RecordedToolInvocation>();
        }

        // Cópia defensiva (o kernel pode ser reutilizado em outro contexto na mesma instância de serviço de LLM
        // em arquitecturas futuras; hoje o kernel é por invocação).
        return [.. list];
    }
}
