using ECommerce.AgentAPI.ECommerceClient.Dtos;
using Refit;

namespace ECommerce.AgentAPI.ECommerceClient;

/// <summary>
/// Cliente Refit para a API e-commerce. O <c>BaseAddress</c> do HttpClient deve ser o prefixo da API <strong>sem</strong> barra no fim
/// (ex.: <c>http://localhost:5149/api/v1</c>), para não duplicar <c>/</c> ao juntar com rotas como <c>/cart</c>.
/// O Refit exige que cada rota comece com <c>/</c>.
/// </summary>
public interface IECommerceApi
{
    [Get("/products")]
    Task<ECommerceApiResponse<PagedResult<ProductDto>>> GetProductsAsync([Query] ProductQueryParams query);

    [Get("/products/{id}")]
    Task<ECommerceApiResponse<ProductDto>> GetProductByIdAsync(Guid id);

    [Get("/cart")]
    Task<ECommerceApiResponse<CartDto>> GetCartAsync();

    [Post("/cart/items")]
    Task<ECommerceApiResponse<CartDto>> AddCartItemAsync([Body] AddCartItemDto dto);

    [Put("/cart/items/{id}")]
    Task<ECommerceApiResponse<CartDto>> UpdateCartItemAsync(Guid id, [Body] UpdateCartItemDto dto);

    [Delete("/cart/items/{id}")]
    Task<ECommerceApiResponse<CartDto>> RemoveCartItemAsync(Guid id);

    [Delete("/cart")]
    Task<IApiResponse> ClearCartAsync();

    [Get("/orders")]
    Task<ECommerceApiResponse<PagedResult<OrderSummaryDto>>> GetOrdersAsync([Query] OrderQueryParams query);

    [Get("/orders/{id}")]
    Task<ECommerceApiResponse<OrderDto>> GetOrderByIdAsync(Guid id);
}
