namespace Darkhorse.Domain.Interfaces.Services;

public record StrategyContext(
    IEnumerable<IEnumerable<object>> Ohlcv,
    Dictionary<string, decimal> Balance,
    Dictionary<string, object> Parameters);

public record StrategyOutput(string Signal, double Quantity, string? Reason)
{
    public bool IsValid() =>
        Signal is "BUY" or "SELL" or "HOLD"
        && Quantity >= 0 && Quantity <= 1_000_000
        && (Reason?.Length ?? 0) <= 500;
}

public interface IStrategyRunner
{
    Task<StrategyOutput> RunAsync(string script, StrategyContext context, CancellationToken ct = default);
}
