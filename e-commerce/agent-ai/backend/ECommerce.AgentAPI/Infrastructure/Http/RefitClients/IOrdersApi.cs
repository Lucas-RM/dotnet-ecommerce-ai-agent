using ECommerce.AgentAPI.ECommerceClient.Dtos;
using Refit;

namespace ECommerce.AgentAPI.ECommerceClient;

public interface IOrdersApi
{
    [Get("/orders")]
    Task<ECommerceApiResponse<PagedResult<OrderSummaryDto>>> GetOrdersAsync([Query] OrderQueryParams query);

    [Get("/orders/{id}")]
    Task<ECommerceApiResponse<OrderDto>> GetOrderByIdAsync(Guid id);
}
