using Asp.Versioning;
using ECommerce.Application.Common;
using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ECommerce.API.Controllers.v1;

[ApiController]
[ApiVersion("1.0")]
public sealed class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost("api/v{version:apiVersion}/orders/checkout")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Checkout(CancellationToken cancellationToken)
    {
        var order = await orderService.CheckoutAsync(CustomerId(), cancellationToken);
        return Created($"/api/v1/orders/{order.Id}", ApiResponse<OrderDto>.Ok(order));
    }

    [HttpGet("api/v{version:apiVersion}/orders")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> MyOrders([FromQuery] OrderQueryParams query, CancellationToken cancellationToken)
    {
        var page = await orderService.GetMyOrdersAsync(CustomerId(), query, cancellationToken);
        return Ok(ApiResponse<PagedResult<OrderSummaryDto>>.Ok(page));
    }

    [HttpGet("api/v{version:apiVersion}/orders/{id:guid}")]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await orderService.GetByIdAsync(CustomerId(), id, cancellationToken);
        return order is null
            ? NotFound(ApiResponse<string>.Fail("Pedido não encontrado."))
            : Ok(ApiResponse<OrderDto>.Ok(order));
    }

    [HttpGet("api/v{version:apiVersion}/admin/orders")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminList([FromQuery] AdminOrderQueryParams query, CancellationToken cancellationToken)
    {
        var page = await orderService.GetAllOrdersAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResult<OrderSummaryDto>>.Ok(page));
    }

    private Guid CustomerId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
