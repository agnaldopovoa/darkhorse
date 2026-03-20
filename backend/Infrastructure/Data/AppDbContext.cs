using Darkhorse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Darkhorse.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<BrokerCredential> BrokerCredentials => Set<BrokerCredential>();
    public DbSet<Strategy> Strategies => Set<Strategy>();
    public DbSet<StrategyVersion> StrategyVersions => Set<StrategyVersion>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Execution> Executions => Set<Execution>();
    public DbSet<DataHistory> DataHistory => Set<DataHistory>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- User ---
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.NotificationPreferences).HasColumnType("jsonb");
        });

        // --- BrokerCredential ---
        modelBuilder.Entity<BrokerCredential>(e =>
        {
            e.ToTable("broker_credentials");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User).WithMany(u => u.BrokerCredentials)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.BrokerName).HasMaxLength(64).IsRequired();
            e.Property(x => x.FeeRate).HasPrecision(8, 6);
            e.Property(x => x.FundingRate).HasPrecision(8, 6);
            e.Property(x => x.Status).HasMaxLength(32);
        });

        // --- Strategy ---
        modelBuilder.Entity<Strategy>(e =>
        {
            e.ToTable("strategies");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User).WithMany(u => u.Strategies)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Credential).WithMany(c => c.Strategies)
             .HasForeignKey(x => x.CredentialId).OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.Parameters).HasColumnType("jsonb");
            e.Property(x => x.Mode).HasMaxLength(16);
            e.Property(x => x.Status).HasMaxLength(16);
            e.Property(x => x.CircuitState).HasMaxLength(16);
            e.Property(x => x.MaxPositionSize).HasPrecision(24, 8);
            e.Property(x => x.MaxDailyVolume).HasPrecision(24, 8);
        });

        // --- StrategyVersion ---
        modelBuilder.Entity<StrategyVersion>(e =>
        {
            e.ToTable("strategy_versions");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Strategy).WithMany(s => s.Versions)
             .HasForeignKey(x => x.StrategyId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Parameters).HasColumnType("jsonb");
            e.HasIndex(x => new { x.StrategyId, x.Version }).IsUnique();
        });

        // --- Order ---
        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Strategy).WithMany(s => s.Orders)
             .HasForeignKey(x => x.StrategyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.User).WithMany()
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            e.Property(x => x.Quantity).HasPrecision(24, 8);
            e.Property(x => x.RequestedPrice).HasPrecision(24, 8);
            e.Property(x => x.FillPrice).HasPrecision(24, 8);
            e.Property(x => x.FillQuantity).HasPrecision(24, 8);
            e.Property(x => x.Fees).HasPrecision(24, 8);
        });

        // --- Execution ---
        modelBuilder.Entity<Execution>(e =>
        {
            e.ToTable("executions");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Strategy).WithMany(s => s.Executions)
             .HasForeignKey(x => x.StrategyId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.ContextSnapshot).HasColumnType("jsonb");
            e.Property(x => x.OutputRaw).HasColumnType("jsonb");
        });

        // --- DataHistory ---
        modelBuilder.Entity<DataHistory>(e =>
        {
            e.ToTable("data_history");
            e.HasKey(x => x.Id);
            e.Property(x => x.Open).HasPrecision(24, 8);
            e.Property(x => x.High).HasPrecision(24, 8);
            e.Property(x => x.Low).HasPrecision(24, 8);
            e.Property(x => x.Close).HasPrecision(24, 8);
            e.Property(x => x.Volume).HasPrecision(32, 8);
            e.HasIndex(x => new { x.Exchange, x.Symbol, x.Timeframe, x.Ts }).IsUnique();
        });

        // --- Notification ---
        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User).WithMany(u => u.Notifications)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Metadata).HasColumnType("jsonb");
        });

        // --- AuditLog ---
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.HasOne(x => x.User).WithMany(u => u.AuditLogs)
             .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
            e.Property(x => x.Metadata).HasColumnType("jsonb");
        });
    }
}
