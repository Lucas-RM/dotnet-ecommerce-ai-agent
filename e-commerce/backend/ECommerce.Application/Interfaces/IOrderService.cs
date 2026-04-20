using ECommerce.Application.Common;
using ECommerce.Application.DTOs;

namespace ECommerce.Application.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CheckoutAsync(Guid userId, CancellationToken cancellationToken);

    Task<PagedResult<OrderSummaryDto>> GetMyOrdersAsync(Guid userId, OrderQueryParams query, CancellationToken cancellationToken);

    Task<OrderDto?> GetByIdAsync(Guid userId, Guid orderId, CancellationToken cancellationToken);

    Task<PagedResult<OrderSummaryDto>> GetAllOrdersAsync(AdminOrderQueryParams query, CancellationToken cancellationToken);
}
