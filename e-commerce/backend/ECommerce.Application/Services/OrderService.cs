using ECommerce.Application.Common;
using ECommerce.Application.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Domain.Exceptions;

namespace ECommerce.Application.Services;

public sealed class OrderService(ICartRepository carts, IProductRepository products, IOrderRepository orders, IUnitOfWork uow) : IOrderService
{
    public Task<OrderDto> CheckoutAsync(Guid userId, CancellationToken cancellationToken) =>
        uow.ExecuteInSerializableTransactionAsync(async () =>
        {
            var cart = await carts.GetWithItemsByUserIdAsync(userId, cancellationToken);
            if (cart is null || cart.Items.Count == 0)
            {
                throw new DomainException("O carrinho está vazio.");
            }

            var productIds = cart.Items.Select(i => i.ProductId).ToList();
            var productList = await products.GetByIdsTrackedAsync(productIds, cancellationToken);
            var byId = productList.ToDictionary(p => p.Id);

            foreach (var line in cart.Items)
            {
                if (!byId.TryGetValue(line.ProductId, out var p))
                {
                    throw new NotFoundDomainException("Produto não encontrado no pedido.");
                }

                if (!p.IsActive)
                {
                    throw new DomainException($"O produto '{p.Name}' não está mais disponível.");
                }

                if (p.StockQuantity < line.Quantity)
                {
                    throw new DomainException($"Estoque insuficiente para '{p.Name}'.");
                }
            }

            var order = new Order
            {
                UserId = userId,
                PlacedAt = DateTime.UtcNow,
                Status = OrderStatus.Placed,
                Items = []
            };

            decimal total = 0;
            foreach (var line in cart.Items)
            {
                var p = byId[line.ProductId];
                var unit = p.Price;
                total += unit * line.Quantity;
                order.Items.Add(new OrderItem
                {
                    ProductId = p.Id,
                    Quantity = line.Quantity,
                    UnitPrice = unit
                });
                p.StockQuantity -= line.Quantity;
            }

            order.TotalAmount = total;
            cart.Items.Clear();
            await orders.AddAsync(order, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            var created = await orders.GetByIdWithItemsForUserAsync(order.Id, userId, cancellationToken);
            return MapOrder(created!);
        }, cancellationToken);

    public async Task<PagedResult<OrderSummaryDto>> GetMyOrdersAsync(Guid userId, OrderQueryParams query, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var page = Math.Max(1, query.Page);
        var (items, total) = await orders.GetPagedByUserAsync(userId, page, pageSize, cancellationToken);
        return new PagedResult<OrderSummaryDto>
        {
            Items = items.Select(MapSummary).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OrderDto?> GetByIdAsync(Guid userId, Guid orderId, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdWithItemsForUserAsync(orderId, userId, cancellationToken);
        return order is null ? null : MapOrder(order);
    }

    public async Task<PagedResult<OrderSummaryDto>> GetAllOrdersAsync(AdminOrderQueryParams query, CancellationToken cancellationToken)
    {
        var pageSize = Math.Clamp(query.PageSize, 1, 50);
        var page = Math.Max(1, query.Page);
        var (items, total) = await orders.GetPagedAdminAsync(page, pageSize, query.UserId, query.Status, cancellationToken);
        return new PagedResult<OrderSummaryDto>
        {
            Items = items.Select(MapSummary).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    private static OrderSummaryDto MapSummary(Order o) =>
        new(o.Id, o.PlacedAt, o.Status.ToString(), o.TotalAmount);

    private static OrderDto MapOrder(Order o)
    {
        var items = o.Items.Select(i =>
        {
            var name = i.Product?.Name ?? string.Empty;
            return new OrderItemDto(i.ProductId, name, i.Quantity, i.UnitPrice, i.UnitPrice * i.Quantity);
        }).ToList();
        return new OrderDto(o.Id, o.PlacedAt, o.Status.ToString(), o.TotalAmount, items);
    }
}
