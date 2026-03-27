using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Domain.Interfaces.Services;
using Darkhorse.Infrastructure.Cache;
using Darkhorse.Infrastructure.Data;
using Darkhorse.Infrastructure.Repositories;
using Darkhorse.Infrastructure.Resilience;
using Darkhorse.Infrastructure.Security;
using Darkhorse.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Darkhorse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DB_CONNECTION")
            ?? Environment.GetEnvironmentVariable("DB_CONNECTION");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBrokerCredentialRepository, BrokerCredentialRepository>();
        services.AddScoped<IStrategyRepository, StrategyRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IExecutionRepository, ExecutionRepository>();
        services.AddScoped<IDataHistoryRepository, DataHistoryRepository>();

        // Redis
        var redisUrl = configuration["REDIS_URL"] ?? Environment.GetEnvironmentVariable("REDIS_URL");
        if (!string.IsNullOrEmpty(redisUrl))
        {
            var multiplexer = ConnectionMultiplexer.Connect(redisUrl);
            services.AddSingleton<IConnectionMultiplexer>(multiplexer);
            services.AddSingleton<RedisCacheService>();
        }

        // Security
        services.AddSingleton<IPasswordService, PasswordService>();

        var masterKey = configuration["MASTER_ENCRYPTION_KEY"] ?? Environment.GetEnvironmentVariable("MASTER_ENCRYPTION_KEY");
        if (!string.IsNullOrEmpty(masterKey) && masterKey.Length == 64)
        {
            services.AddSingleton<ICredentialEncryption>(new CredentialEncryption(masterKey));
        }

        // Services & Resilience
        services.AddScoped<IBrokerService, BrokerService>();
        services.AddScoped<IStrategyRunner, StrategyExecutor>();
        services.AddSingleton<CircuitBreakerFactory>();

        return services;
    }
}
