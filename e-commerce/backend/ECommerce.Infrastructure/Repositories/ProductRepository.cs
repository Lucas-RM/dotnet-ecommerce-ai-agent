using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class ProductRepository(AppDbContext db) : IProductRepository
{
    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(ProductQueryParams query, CancellationToken cancellationToken)
    {
        var source = db.Products.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            source = source.Where(x => x.Category == query.Category);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            source = source.Where(x => x.Name.Contains(query.Search));
        }

        var total = await source.CountAsync(cancellationToken);
        var items = await source.OrderByDescending(x => x.CreatedAt)
                                    .Skip((query.Page - 1) * query.PageSize)
                                    .Take(query.PageSize)
                                    .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Product?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken cancellationToken) =>
        db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByIdsTrackedAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        return await db.Products.Where(p => idList.Contains(p.Id)).ToListAsync(cancellationToken);
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken) =>
        db.Products.AddAsync(product, cancellationToken).AsTask();
}
