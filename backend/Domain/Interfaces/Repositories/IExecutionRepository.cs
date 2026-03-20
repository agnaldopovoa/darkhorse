using Darkhorse.Domain.Entities;

namespace Darkhorse.Domain.Interfaces.Repositories;

public interface IExecutionRepository
{
    Task<IEnumerable<Execution>> GetByStrategyIdAsync(
        Guid strategyId,
        string? mode = null,
        int limit = 50,
        CancellationToken ct = default);
    Task AddAsync(Execution execution, CancellationToken ct = default);
}
