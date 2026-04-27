using ECommerce.AgentAPI.ECommerceClient.Dtos;
using Refit;

namespace ECommerce.AgentAPI.ECommerceClient;

public interface ICheckoutApi
{
    [Post("/orders/checkout")]
    Task<ECommerceApiResponse<OrderDto>> CheckoutAsync();
}
