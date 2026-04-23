namespace ECommerce.AgentAPI.ECommerceClient.Dtos;

public sealed record CartItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal);

public sealed record CartDto(
    IReadOnlyList<CartItemDto> Items,
    decimal TotalPrice);

public sealed record AddCartItemDto(Guid ProductId, int Quantity);

public sealed record UpdateCartItemDto(int Quantity);
