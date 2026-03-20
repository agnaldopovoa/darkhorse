namespace Darkhorse.Domain.Entities;

public class BrokerCredential
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string BrokerName { get; set; } = string.Empty;  // "binance" | "kucoin" | "coinbase"

    // AES-256-GCM encrypted API Key
    public byte[] ApiKeyNonce { get; set; } = [];
    public byte[] ApiKeyCipher { get; set; } = [];
    public byte[] ApiKeyTag { get; set; } = [];

    // AES-256-GCM encrypted Secret
    public byte[] SecretNonce { get; set; } = [];
    public byte[] SecretCipher { get; set; } = [];
    public byte[] SecretTag { get; set; } = [];

    public int KeyVersion { get; set; } = 1;
    public decimal FeeRate { get; set; } = 0;
    public decimal FundingRate { get; set; } = 0;
    public bool IsSandbox { get; set; } = false;
    public string Status { get; set; } = "active"; // active | revoked | error
    public DateTimeOffset? LastTestedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<Strategy> Strategies { get; set; } = [];
}
