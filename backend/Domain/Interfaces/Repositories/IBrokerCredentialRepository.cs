using Darkhorse.Domain.Entities;

namespace Darkhorse.Domain.Interfaces.Repositories;

public interface IBrokerCredentialRepository
{
    Task<BrokerCredential?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<BrokerCredential>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task AddAsync(BrokerCredential credential, CancellationToken ct = default);
    Task UpdateAsync(BrokerCredential credential, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
