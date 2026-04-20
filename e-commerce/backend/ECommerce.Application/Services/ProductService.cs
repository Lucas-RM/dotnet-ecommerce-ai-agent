using ECommerce.Application.Common;
using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Services;

public sealed class ProductService(IProductRepository products, IUnitOfWork uow) : IProductService
{
    public async Task<PagedResult<ProductDto>> GetPagedAsync(ProductQueryParams query, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var normalized = query with { Page = Math.Max(1, query.Page), PageSize = pageSize };
        var (items, total) = await products.GetPagedAsync(normalized, cancellationToken);
        return new PagedResult<ProductDto>
        {
            Items = items.Select(Map).ToList(),
            TotalCount = total,
            Page = normalized.Page,
            PageSize = normalized.PageSize
        };
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(id, cancellationToken);
        return product is null ? null : Map(product);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken)
    {
        var entity = new Product { Name = dto.Name.Trim(), Description = dto.Description.Trim(), Price = dto.Price, StockQuantity = dto.StockQuantity, Category = dto.Category.Trim() };
        await products.AddAsync(entity, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken)
    {
        var entity = await products.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        entity.Name = dto.Name?.Trim() ?? entity.Name;
        entity.Description = dto.Description?.Trim() ?? entity.Description;
        entity.Price = dto.Price ?? entity.Price;
        entity.StockQuantity = dto.StockQuantity ?? entity.StockQuantity;
        entity.Category = dto.Category?.Trim() ?? entity.Category;
        entity.IsActive = dto.IsActive ?? entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(cancellationToken);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await products.GetByIdAsync(id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsActive = false;
        entity.UpdatedAt = DateTime.UtcNow;
        await uow.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static ProductDto Map(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.StockQuantity, p.Category, p.IsActive);
}
