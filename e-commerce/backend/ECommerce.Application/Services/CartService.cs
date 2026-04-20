using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Application.Services;

public sealed class CartService(ICartRepository carts, IProductRepository products, IUnitOfWork uow) : ICartService
{
    public async Task<CartDto> GetCartAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cart = await carts.GetWithItemsAndProductsByUserIdAsync(userId, cancellationToken);
        return cart is null ? new CartDto([], 0) : MapCart(cart);
    }

    public Task<CartDto> AddItemAsync(Guid userId, AddCartItemDto dto, CancellationToken cancellationToken) =>
        ExecuteWithSingleConcurrencyRetryAsync(() =>
            uow.ExecuteInSerializableTransactionAsync(async () =>
            {
                var product = await products.GetByIdAsNoTrackingAsync(dto.ProductId, cancellationToken)
                    ?? throw new NotFoundDomainException("Produto não encontrado.");
                if (!product.IsActive)
                {
                    throw new DomainException("Produto indisponível.");
                }

                var cart = await GetOrCreateCartAsync(userId, cancellationToken);
                var existing = await carts.GetItemByCartAndProductAsync(cart.Id, dto.ProductId, cancellationToken);
                var newQty = (existing?.Quantity ?? 0) + dto.Quantity;
                if (newQty > product.StockQuantity)
                {
                    throw new DomainException("Quantidade no carrinho não pode exceder o estoque disponível.");
                }

                if (existing is null)
                {
                    await carts.AddItemAsync(new CartItem
                    {
                        CartId = cart.Id,
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity
                    }, cancellationToken);
                }
                else
                {
                    existing.Quantity = newQty;
                }

                await uow.SaveChangesAsync(cancellationToken);
                var reloaded = await carts.GetWithItemsAndProductsByUserIdAsync(userId, cancellationToken);
                return MapCart(reloaded!);
            }, cancellationToken), cancellationToken);

    public Task<CartDto> UpdateItemAsync(Guid userId, Guid productId, UpdateCartItemDto dto, CancellationToken cancellationToken) =>
        ExecuteWithSingleConcurrencyRetryAsync(() =>
            uow.ExecuteInSerializableTransactionAsync(async () =>
            {
                var cart = await carts.GetWithItemsByUserIdAsync(userId, cancellationToken)
                    ?? throw new NotFoundDomainException("Carrinho não encontrado.");
                var line = cart.Items.FirstOrDefault(i => i.ProductId == productId)
                    ?? throw new NotFoundDomainException("Item não encontrado no carrinho.");

                var product = await products.GetByIdAsNoTrackingAsync(productId, cancellationToken)
                    ?? throw new NotFoundDomainException("Produto não encontrado.");
                if (!product.IsActive)
                {
                    throw new DomainException("Produto indisponível.");
                }

                if (dto.Quantity > product.StockQuantity)
                {
                    throw new DomainException("Quantidade no carrinho não pode exceder o estoque disponível.");
                }

                line.Quantity = dto.Quantity;
                await uow.SaveChangesAsync(cancellationToken);
                var reloaded = await carts.GetWithItemsAndProductsByUserIdAsync(userId, cancellationToken);
                return MapCart(reloaded!);
            }, cancellationToken), cancellationToken);

    public Task<CartDto> RemoveItemAsync(Guid userId, Guid productId, CancellationToken cancellationToken) =>
        ExecuteWithSingleConcurrencyRetryAsync(() =>
            uow.ExecuteInSerializableTransactionAsync(async () =>
            {
                var cart = await carts.GetWithItemsByUserIdAsync(userId, cancellationToken)
                    ?? throw new NotFoundDomainException("Carrinho não encontrado.");
                var line = await carts.GetItemByCartAndProductAsync(cart.Id, productId, cancellationToken)
                    ?? throw new NotFoundDomainException("Item não encontrado no carrinho.");

                carts.RemoveItem(line);
                await uow.SaveChangesAsync(cancellationToken);
                var reloaded = await carts.GetWithItemsAndProductsByUserIdAsync(userId, cancellationToken);
                return reloaded is null ? new CartDto([], 0) : MapCart(reloaded);
            }, cancellationToken), cancellationToken);

    public Task ClearCartAsync(Guid userId, CancellationToken cancellationToken) =>
        ExecuteWithSingleConcurrencyRetryAsync(() =>
            uow.ExecuteInSerializableTransactionAsync(async () =>
            {
                var cart = await carts.GetWithItemsByUserIdAsync(userId, cancellationToken);
                if (cart is null)
                {
                    return;
                }

                var items = await carts.GetItemsByCartIdAsync(cart.Id, cancellationToken);
                if (items.Count == 0)
                {
                    return;
                }

                // A limpeza do carrinho é idempotente: em corrida entre requests, itens já removidos não devem quebrar o fluxo.
                carts.RemoveItems(items);
                await uow.SaveChangesAsync(cancellationToken);
            }, cancellationToken), cancellationToken);

    private async Task<TResult> ExecuteWithSingleConcurrencyRetryAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            uow.ClearTracking();
            return await operation();
        }
        catch (Exception ex) when (IsConcurrencyException(ex) && !cancellationToken.IsCancellationRequested)
        {
            // Reexecuta com contexto limpo para evitar replay com entidades em estado stale.
            uow.ClearTracking();
            return await operation();
        }
    }

    private async Task ExecuteWithSingleConcurrencyRetryAsync(
        Func<Task> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            uow.ClearTracking();
            await operation();
        }
        catch (Exception ex) when (IsConcurrencyException(ex) && !cancellationToken.IsCancellationRequested)
        {
            // Reexecuta com contexto limpo para evitar replay com entidades em estado stale.
            uow.ClearTracking();
            await operation();
        }
    }

    private static bool IsConcurrencyException(Exception ex) =>
        ex.GetType().Name == "DbUpdateConcurrencyException";

    /// <summary>
    /// Garante um carrinho rastreado sem incluir <see cref="Product"/> nos itens, evitando conflito com validações que usam produto em AsNoTracking.
    /// Novo carrinho é persistido no mesmo <see cref="uow.SaveChangesAsync"/> do chamador (add item).
    /// </summary>
    private async Task<Cart> GetOrCreateCartAsync(Guid userId, CancellationToken cancellationToken)
    {
        var cart = await carts.GetWithItemsByUserIdAsync(userId, cancellationToken);
        if (cart is not null)
        {
            return cart;
        }

        cart = new Cart { UserId = userId };
        await carts.AddCartAsync(cart, cancellationToken);
        return cart;
    }

    private static CartDto MapCart(Cart cart)
    {
        var items = cart.Items.Select(i =>
        {
            var unit = i.Product?.Price ?? 0;
            var name = i.Product?.Name ?? string.Empty;
            return new CartItemDto(i.ProductId, name, unit, i.Quantity, unit * i.Quantity);
        }).ToList();
        var total = items.Sum(x => x.Subtotal);
        return new CartDto(items, total);
    }
}
