namespace Darkhorse.Domain.Entities;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Type { get; set; } = string.Empty;      // circuit_breaker | order_fill | auth_event | system
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public bool IsRead { get; set; } = false;
    public string Metadata { get; set; } = "{}";          // JSONB
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
