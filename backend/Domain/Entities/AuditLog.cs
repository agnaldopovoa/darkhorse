namespace Darkhorse.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;      // e.g. "broker_created", "strategy_started"
    public string? EntityType { get; set; }                 // e.g. "strategy", "order"
    public Guid? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Metadata { get; set; } = "{}";            // JSONB — non-sensitive context
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User? User { get; set; }
}
