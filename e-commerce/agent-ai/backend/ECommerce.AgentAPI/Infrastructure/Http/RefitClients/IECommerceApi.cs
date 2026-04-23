using ECommerce.AgentAPI.ECommerceClient.Dtos;
using Refit;

namespace ECommerce.AgentAPI.ECommerceClient;

/// <summary>
/// Cliente Refit para a <strong>API pública e de cliente</strong> do e-commerce (produtos, carrinho, pedidos e checkout).
/// Rotas de <strong>admin, auth, registo</strong> e similares não estão mapeadas — o Agent não as invoca.
/// <para>
/// O <c>BaseAddress</c> do <see cref="System.Net.Http.HttpClient"/> deve ser o prefixo <strong>sem</strong> barra no fim
/// (ex.: <c>http://localhost:7026/api/v1</c>); o Refit exige que cada rota comece com <c>/</c>.
/// </para>
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

    [Post("/orders/checkout")]
    Task<ECommerceApiResponse<OrderDto>> CheckoutAsync();
}
