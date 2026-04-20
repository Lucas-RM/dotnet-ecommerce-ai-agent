using ECommerce.Application.Common;
using ECommerce.Application.DTOs;

namespace ECommerce.Application.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(ProductQueryParams query, CancellationToken cancellationToken);

    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellationToken);

    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto, CancellationToken cancellationToken);

    Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken);
}
