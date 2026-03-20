using Darkhorse.Domain.Entities;

namespace Darkhorse.Domain.Interfaces.Repositories;

public interface IStrategyRepository
{
    Task<Strategy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Strategy>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IEnumerable<Strategy>> GetAllRunningAsync(CancellationToken ct = default);
    Task AddAsync(Strategy strategy, CancellationToken ct = default);
    Task UpdateAsync(Strategy strategy, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
