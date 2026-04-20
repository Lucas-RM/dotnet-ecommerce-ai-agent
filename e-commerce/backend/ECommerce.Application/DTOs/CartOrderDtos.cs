using ECommerce.Domain.Enums;

namespace ECommerce.Application.DTOs;

public sealed record CartItemDto(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal Subtotal);
public sealed record CartDto(IReadOnlyList<CartItemDto> Items, decimal TotalPrice);

public sealed record AddCartItemDto(Guid ProductId, int Quantity);
public sealed record UpdateCartItemDto(int Quantity);

public sealed record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal Subtotal);
public sealed record OrderDto(Guid Id, DateTime PlacedAt, string Status, decimal TotalAmount, IReadOnlyList<OrderItemDto> Items);
public sealed record OrderSummaryDto(Guid Id, DateTime PlacedAt, string Status, decimal TotalAmount);

public sealed record OrderQueryParams(int Page = 1, int PageSize = 10);
public sealed record AdminOrderQueryParams(int Page = 1, int PageSize = 10, Guid? UserId = null, OrderStatus? Status = null);
