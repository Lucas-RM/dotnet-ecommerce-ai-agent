namespace ECommerce.AgentAPI.ECommerceClient.Dtos;

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record OrderDto(
    Guid Id,
    DateTime PlacedAt,
    string Status,
    decimal TotalAmount,
    IReadOnlyList<OrderItemDto> Items);

public sealed record OrderSummaryDto(
    Guid Id,
    DateTime PlacedAt,
    string Status,
    decimal TotalAmount);

public sealed record OrderQueryParams(int Page = 1, int PageSize = 10);
