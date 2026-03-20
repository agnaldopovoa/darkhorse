namespace Darkhorse.Domain.Entities;

public class DataHistory
{
    public long Id { get; set; }
    public string Exchange { get; set; } = string.Empty;     // e.g. "binance"
    public string Symbol { get; set; } = string.Empty;      // e.g. "BTC/USDT"
    public string Timeframe { get; set; } = string.Empty;   // e.g. "1h", "15m", "1d"
    public DateTimeOffset Ts { get; set; }                  // candle open timestamp
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}
