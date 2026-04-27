using ECommerce.AgentAPI.ECommerceClient.Dtos;
using Refit;

namespace ECommerce.AgentAPI.ECommerceClient;

public interface ICartApi
{
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
}
