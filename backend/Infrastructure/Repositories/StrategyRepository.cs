using Darkhorse.Domain.Entities;
using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Darkhorse.Infrastructure.Repositories;

public class StrategyRepository(AppDbContext db) : IStrategyRepository
{
    public Task<Strategy?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Strategies.Include(s => s.Credential).FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<IEnumerable<Strategy>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => db.Strategies.Where(s => s.UserId == userId).OrderByDescending(s => s.UpdatedAt)
             .ToListAsync(ct)
             .ContinueWith(t => t.Result.AsEnumerable(), ct);

    public Task<IEnumerable<Strategy>> GetAllRunningAsync(CancellationToken ct = default)
        => db.Strategies.Where(s => s.Status == "running" && s.CircuitState != "OPEN")
             .Include(s => s.Credential)
             .ToListAsync(ct)
             .ContinueWith(t => t.Result.AsEnumerable(), ct);

    public async Task AddAsync(Strategy strategy, CancellationToken ct = default)
    {
        db.Strategies.Add(strategy);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Strategy strategy, CancellationToken ct = default)
    {
        strategy.UpdatedAt = DateTimeOffset.UtcNow;
        db.Strategies.Update(strategy);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Strategies.FindAsync([id], ct);
        if (entity is not null)
        {
            db.Strategies.Remove(entity);
            await db.SaveChangesAsync(ct);
        }
    }
}
