namespace ECommerce.AgentAPI.ECommerceClient.Dtos;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    string Category,
    bool IsActive);

public sealed record ProductQueryParams(
    int Page = 1,
    int PageSize = 10,
    string? Category = null,
    string? Search = null);
