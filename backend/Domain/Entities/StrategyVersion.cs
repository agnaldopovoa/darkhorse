namespace Darkhorse.Domain.Entities;

public class StrategyVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StrategyId { get; set; }
    public int Version { get; set; }
    public string Script { get; set; } = string.Empty;
    public string Parameters { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public Strategy Strategy { get; set; } = null!;
}
