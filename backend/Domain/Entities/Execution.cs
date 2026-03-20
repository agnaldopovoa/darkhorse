namespace Darkhorse.Domain.Entities;

public class Execution
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StrategyId { get; set; }
    public int? ScriptVersion { get; set; }
    public string Signal { get; set; } = "HOLD";          // BUY | SELL | HOLD
    public string? SignalReason { get; set; }
    public string Mode { get; set; } = string.Empty;      // live | paper | backtest
    public string? ContextSnapshot { get; set; }          // JSONB: OHLCV window + balance
    public string? OutputRaw { get; set; }                // JSONB: raw container stdout output
    public string? ErrorMessage { get; set; }
    public int? DurationMs { get; set; }
    public int? ContainerExitCode { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Strategy Strategy { get; set; } = null!;
}
