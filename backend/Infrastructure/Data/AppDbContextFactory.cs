using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Darkhorse.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // 1. Build configuration (Environment Variables)
        // Note: dotnet ef does not automatically load .env files
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION")
            ?? "Host=localhost;Port=5431;Database=darkhorse_dev;Username=darkhorse;Password=darkhorse";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
