namespace ECommerce.Application.DTOs;
public sealed record ProductDto(Guid Id, string Name, string Description, decimal Price, int StockQuantity, string Category, bool IsActive);
public sealed record CreateProductDto(string Name, string Description, decimal Price, int StockQuantity, string Category);
public sealed record UpdateProductDto(string? Name, string? Description, decimal? Price, int? StockQuantity, string? Category, bool? IsActive);
public sealed record ProductQueryParams(int Page = 1, int PageSize = 10, string? Category = null, string? Search = null);
