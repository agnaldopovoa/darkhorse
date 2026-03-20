using Darkhorse.Domain.Entities;
using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Darkhorse.Infrastructure.Repositories;

public class ExecutionRepository(AppDbContext db) : IExecutionRepository
{
    public async Task<IEnumerable<Execution>> GetByStrategyIdAsync(
        Guid strategyId, string? mode = null, int limit = 50, CancellationToken ct = default)
    {
        var query = db.Executions.Where(e => e.StrategyId == strategyId).AsQueryable();
        if (!string.IsNullOrEmpty(mode)) query = query.Where(e => e.Mode == mode);
        return await query.OrderByDescending(e => e.CreatedAt).Take(limit).ToListAsync(ct);
    }

    public async Task AddAsync(Execution execution, CancellationToken ct = default)
    {
        db.Executions.Add(execution);
        await db.SaveChangesAsync(ct);
    }
}
