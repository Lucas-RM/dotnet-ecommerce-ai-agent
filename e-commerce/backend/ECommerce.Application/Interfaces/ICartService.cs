using ECommerce.Application.DTOs;

namespace ECommerce.Application.Interfaces;

public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid userId, CancellationToken cancellationToken);

    Task<CartDto> AddItemAsync(Guid userId, AddCartItemDto dto, CancellationToken cancellationToken);

    Task<CartDto> UpdateItemAsync(Guid userId, Guid productId, UpdateCartItemDto dto, CancellationToken cancellationToken);

    Task<CartDto> RemoveItemAsync(Guid userId, Guid productId, CancellationToken cancellationToken);

    Task ClearCartAsync(Guid userId, CancellationToken cancellationToken);
}
