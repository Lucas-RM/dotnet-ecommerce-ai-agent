using ECommerce.AgentAPI.ECommerceClient;

namespace ECommerce.AgentAPI.Infrastructure.LLM;

public interface IKernelFactory
{
    Microsoft.SemanticKernel.Kernel CreateKernel(IECommerceApi ecommerceApi, string sessionId);
}
