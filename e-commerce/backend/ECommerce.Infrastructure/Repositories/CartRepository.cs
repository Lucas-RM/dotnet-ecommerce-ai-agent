using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class CartRepository(AppDbContext db) : ICartRepository
{
    public Task<Cart?> GetWithItemsByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public Task<Cart?> GetWithItemsAndProductsByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        db.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public Task<CartItem?> GetItemByCartAndProductAsync(Guid cartId, Guid productId, CancellationToken cancellationToken) =>
        db.CartItems.FirstOrDefaultAsync(i => i.CartId == cartId && i.ProductId == productId, cancellationToken);

    public async Task<IReadOnlyList<CartItem>> GetItemsByCartIdAsync(Guid cartId, CancellationToken cancellationToken) =>
        await db.CartItems.Where(i => i.CartId == cartId).ToListAsync(cancellationToken);

    public Task AddCartAsync(Cart cart, CancellationToken cancellationToken) =>
        db.Carts.AddAsync(cart, cancellationToken).AsTask();

    public Task AddItemAsync(CartItem item, CancellationToken cancellationToken) =>
        db.CartItems.AddAsync(item, cancellationToken).AsTask();

    public void RemoveItem(CartItem item) =>
        db.CartItems.Remove(item);

    public void RemoveItems(IEnumerable<CartItem> items) =>
        db.CartItems.RemoveRange(items);
}
