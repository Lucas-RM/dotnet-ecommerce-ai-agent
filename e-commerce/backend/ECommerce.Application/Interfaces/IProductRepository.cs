using ECommerce.Application.DTOs;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces;

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(ProductQueryParams query, CancellationToken cancellationToken);

    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Leitura sem rastreamento 
    /// (ex.: validação de estoque no carrinho sem conflitar com o grafo do <see cref="Cart"/>).
    /// </summary>
    Task<Product?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Product>> GetByIdsTrackedAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);
}
