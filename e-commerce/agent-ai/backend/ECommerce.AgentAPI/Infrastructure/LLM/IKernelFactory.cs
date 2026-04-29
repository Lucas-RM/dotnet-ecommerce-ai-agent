namespace ECommerce.AgentAPI.Infrastructure.LLM;

public interface IKernelFactory
{
    Microsoft.SemanticKernel.Kernel CreateKernel(
        string sessionId,
        IServiceProvider requestServices);
}
