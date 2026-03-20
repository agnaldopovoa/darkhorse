namespace Darkhorse.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;   // Argon2id output (hex)
    public string PasswordSalt { get; set; } = string.Empty;   // hex-encoded random salt
    public bool IsActive { get; set; } = true;
    public string NotificationPreferences { get; set; } = "{}"; // jsonb
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public ICollection<BrokerCredential> BrokerCredentials { get; set; } = [];
    public ICollection<Strategy> Strategies { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
