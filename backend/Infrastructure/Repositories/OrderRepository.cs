using Darkhorse.Domain.Entities;
using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Darkhorse.Infrastructure.Repositories;

public class OrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Orders.FindAsync([id], ct).AsTask();

    public async Task<IEnumerable<Order>> GetFilteredAsync(
        Guid userId, string? mode, string? status, string? symbol,
        DateTimeOffset? from, DateTimeOffset? to, int limit = 100, CancellationToken ct = default)
    {
        var query = db.Orders.Where(o => o.UserId == userId).AsQueryable();
        if (!string.IsNullOrEmpty(mode)) query = query.Where(o => o.Mode == mode);
        if (!string.IsNullOrEmpty(status)) query = query.Where(o => o.Status == status);
        if (!string.IsNullOrEmpty(symbol)) query = query.Where(o => o.Symbol == symbol);
        if (from.HasValue) query = query.Where(o => o.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(o => o.CreatedAt <= to.Value);
        return await query.OrderByDescending(o => o.CreatedAt).Take(limit).ToListAsync(ct);
    }

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        db.Orders.Update(order);
        await db.SaveChangesAsync(ct);
    }
}
