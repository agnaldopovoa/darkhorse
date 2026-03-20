using Darkhorse.Domain.Entities;
using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Darkhorse.Infrastructure.Repositories;

public class DataHistoryRepository(AppDbContext db) : IDataHistoryRepository
{
    public async Task<IEnumerable<DataHistory>> GetRangeAsync(
        string exchange, string symbol, string timeframe,
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
        => await db.DataHistory
            .Where(d => d.Exchange == exchange && d.Symbol == symbol
                     && d.Timeframe == timeframe && d.Ts >= from && d.Ts <= to)
            .OrderBy(d => d.Ts)
            .ToListAsync(ct);

    public async Task UpsertBatchAsync(IEnumerable<DataHistory> candles, CancellationToken ct = default)
    {
        // Npgsql supports ON CONFLICT DO UPDATE via ExecuteUpdate or raw SQL
        foreach (var candle in candles)
        {
            var exists = await db.DataHistory.AnyAsync(
                d => d.Exchange == candle.Exchange && d.Symbol == candle.Symbol
                  && d.Timeframe == candle.Timeframe && d.Ts == candle.Ts, ct);

            if (!exists)
                db.DataHistory.Add(candle);
        }
        await db.SaveChangesAsync(ct);
    }
}
