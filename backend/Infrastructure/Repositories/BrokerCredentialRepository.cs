using Darkhorse.Domain.Entities;
using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Darkhorse.Infrastructure.Repositories;

public class BrokerCredentialRepository(AppDbContext db) : IBrokerCredentialRepository
{
    public Task<BrokerCredential?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.BrokerCredentials.FindAsync([id], ct).AsTask();

    public Task<IEnumerable<BrokerCredential>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => db.BrokerCredentials.Where(c => c.UserId == userId)
             .OrderBy(c => c.CreatedAt)
             .ToListAsync(ct)
             .ContinueWith(t => t.Result.AsEnumerable(), ct);

    public async Task AddAsync(BrokerCredential credential, CancellationToken ct = default)
    {
        db.BrokerCredentials.Add(credential);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(BrokerCredential credential, CancellationToken ct = default)
    {
        db.BrokerCredentials.Update(credential);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.BrokerCredentials.FindAsync([id], ct);
        if (entity is not null)
        {
            db.BrokerCredentials.Remove(entity);
            await db.SaveChangesAsync(ct);
        }
    }
}
