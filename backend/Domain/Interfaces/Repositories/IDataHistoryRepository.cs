using Darkhorse.Domain.Entities;

namespace Darkhorse.Domain.Interfaces.Repositories;

public interface IDataHistoryRepository
{
    Task<IEnumerable<DataHistory>> GetRangeAsync(
        string exchange,
        string symbol,
        string timeframe,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);
    Task UpsertBatchAsync(IEnumerable<DataHistory> candles, CancellationToken ct = default);
}
