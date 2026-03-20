namespace Darkhorse.Domain.Interfaces.Services;

public record TickerData(string Symbol, decimal Last, decimal Bid, decimal Ask, decimal Volume);
public record MarketData(string Symbol, string Base, string Quote);
public record OhlcvCandle(DateTimeOffset Ts, decimal Open, decimal High, decimal Low, decimal Close, decimal Volume);
public record OrderResult(string BrokerOrderId, string Status, decimal? FillPrice, decimal? FillQuantity);

public interface IBrokerService
{
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
    Task<IEnumerable<MarketData>> GetMarketsAsync(CancellationToken ct = default);
    Task<TickerData> GetTickerAsync(string symbol, CancellationToken ct = default);
    Task<IEnumerable<OhlcvCandle>> GetOhlcvAsync(string symbol, string timeframe, DateTimeOffset? since = null, CancellationToken ct = default);
    Task<OrderResult> PlaceOrderAsync(string symbol, string side, decimal amount, decimal? price = null, CancellationToken ct = default);
    Task CancelOrderAsync(string brokerId, string symbol, CancellationToken ct = default);
}
