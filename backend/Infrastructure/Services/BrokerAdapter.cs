using ExchangeSharp;

namespace Darkhorse.Infrastructure.Services;

public class BrokerAdapter
{
    private readonly IExchangeAPI _exchange;

    public static Task<BrokerAdapter> CreateAsync(
        string exchangeName, string apiKey, string secret, bool sandbox = false)
    {
        var exchange = ExchangeAPI.GetExchangeAPI(exchangeName);
        exchange.LoadAPIKeysUnsecure(apiKey, secret);
        
        return Task.FromResult(new BrokerAdapter(exchange));
    }

    private BrokerAdapter(IExchangeAPI exchange) => _exchange = exchange;

    public async Task<IEnumerable<ExchangeMarket>> GetMarketsAsync()
    {
        // Fallback for v0.4.3
        var symbols = await _exchange.GetSymbolsAsync();
        return symbols.Select(s => new ExchangeMarket { MarketName = s });
    }

    public async Task<ExchangeTicker> GetTickerAsync(string symbol)
        => await _exchange.GetTickerAsync(symbol);

    public async Task<IEnumerable<MarketCandle>> GetOhlcvAsync(
        string symbol, int periodSeconds, DateTime? startDate)
        => await _exchange.GetCandlesAsync(symbol, periodSeconds, startDate, null, 1000);

    public async Task<ExchangeOrderResult> PlaceOrderAsync(
        string symbol, string side, decimal amount, decimal? price = null)
    {
        var request = new ExchangeOrderRequest
        {
            Symbol = symbol,
            Amount = amount,
            Price = price ?? 0m,
            IsBuy = side.Equals("BUY", StringComparison.OrdinalIgnoreCase),
            OrderType = price.HasValue ? OrderType.Limit : OrderType.Market
        };
        return await _exchange.PlaceOrderAsync(request);
    }

    public async Task CancelOrderAsync(string orderId, string symbol)
        => await _exchange.CancelOrderAsync(orderId, symbol);
}
