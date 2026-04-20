using ECommerce.Domain.Entities;

namespace ECommerce.Application.Interfaces;

public interface ICartRepository
{
    /// <summary>Carrinho com itens rastreados (mutações: add/update/remove/checkout).</summary>
    Task<Cart?> GetWithItemsByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>Carrinho com itens e produtos (ex.: resposta da API com nome/preço).</summary>
    Task<Cart?> GetWithItemsAndProductsByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<CartItem?> GetItemByCartAndProductAsync(Guid cartId, Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyList<CartItem>> GetItemsByCartIdAsync(Guid cartId, CancellationToken cancellationToken);

    Task AddCartAsync(Cart cart, CancellationToken cancellationToken);

    Task AddItemAsync(CartItem item, CancellationToken cancellationToken);

    void RemoveItem(CartItem item);

    void RemoveItems(IEnumerable<CartItem> items);
}
