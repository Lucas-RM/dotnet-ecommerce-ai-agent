using ECommerce.AgentAPI.ECommerceClient.Dtos;
using Refit;

namespace ECommerce.AgentAPI.ECommerceClient;

public interface IProductsApi
{
    [Get("/products")]
    Task<ECommerceApiResponse<PagedResult<ProductDto>>> GetProductsAsync([Query] ProductQueryParams query);

    [Get("/products/{id}")]
    Task<ECommerceApiResponse<ProductDto>> GetProductByIdAsync(Guid id);
}
