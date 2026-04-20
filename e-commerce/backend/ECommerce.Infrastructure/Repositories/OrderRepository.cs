using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Repositories;

public sealed class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task AddAsync(Order order, CancellationToken cancellationToken) =>
        db.Orders.AddAsync(order, cancellationToken).AsTask();

    public Task<Order?> GetByIdWithItemsForUserAsync(Guid orderId, Guid userId, CancellationToken cancellationToken) =>
        db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedByUserAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking().Where(o => o.UserId == userId).OrderByDescending(o => o.PlacedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetPagedAdminAsync(int page, int pageSize, Guid? userId, OrderStatus? status, CancellationToken cancellationToken)
    {
        var query = db.Orders.AsNoTracking().AsQueryable();
        if (userId.HasValue)
        {
            query = query.Where(o => o.UserId == userId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        query = query.OrderByDescending(o => o.PlacedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, total);
    }
}