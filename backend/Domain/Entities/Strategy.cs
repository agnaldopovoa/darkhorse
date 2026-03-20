namespace Darkhorse.Domain.Entities;

public class Strategy
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid? CredentialId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;      // e.g. "BTC/USDT"
    public string Timeframe { get; set; } = string.Empty;   // e.g. "1h", "15m"
    public string Script { get; set; } = string.Empty;      // Python DSL code
    public int ScriptVersion { get; set; } = 1;
    public string Parameters { get; set; } = "{}";          // JSONB
    public string Mode { get; set; } = "paper";             // live | paper | backtest
    public string Status { get; set; } = "paused";          // running | paused | error
    public decimal? MaxPositionSize { get; set; }
    public decimal? MaxDailyVolume { get; set; }
    public string CircuitState { get; set; } = "CLOSED";    // CLOSED | OPEN | HALF-OPEN
    public int CircuitFailures { get; set; } = 0;
    public DateTimeOffset? CircuitOpenedAt { get; set; }
    public int ScheduleInterval { get; set; } = 60;         // seconds between ticks
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public BrokerCredential? Credential { get; set; }
    public ICollection<StrategyVersion> Versions { get; set; } = [];
    public ICollection<Order> Orders { get; set; } = [];
    public ICollection<Execution> Executions { get; set; } = [];
}
