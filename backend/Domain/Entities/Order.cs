namespace Darkhorse.Domain.Entities;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StrategyId { get; set; }
    public Guid UserId { get; set; }
    public string? BrokerOrderId { get; set; }            // exchange confirmation ID
    public string Symbol { get; set; } = string.Empty;
    public string Side { get; set; } = string.Empty;      // BUY | SELL
    public string OrderType { get; set; } = "market";     // market | limit
    public decimal Quantity { get; set; }
    public decimal? RequestedPrice { get; set; }
    public decimal? FillPrice { get; set; }
    public decimal? FillQuantity { get; set; }
    public string Status { get; set; } = string.Empty;    // submitted | pending | filled | cancelled | rejected
    public string Mode { get; set; } = string.Empty;      // live | paper | backtest
    public decimal? Fees { get; set; }
    public string? FeeCurrency { get; set; }
    public string? SignalReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FilledAt { get; set; }

    // Navigation
    public Strategy Strategy { get; set; } = null!;
    public User User { get; set; } = null!;
}
