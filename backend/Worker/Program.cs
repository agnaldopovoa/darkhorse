using Darkhorse.Application;
using Darkhorse.Infrastructure;
using Darkhorse.Worker.Jobs;
using Hangfire;
using Hangfire.PostgreSql;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(conf => conf
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter()));

// Register DI layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Register Jobs
builder.Services.AddScoped<TickStrategiesJob>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? Environment.GetEnvironmentVariable("DefaultConnection");

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(connectionString)));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = Environment.ProcessorCount * 2;
    options.Queues = ["default", "backtest", "notifications"];
});

var host = builder.Build();

// Setup recurring jobs explicitly
using (var scope = host.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
    recurringJobManager.AddOrUpdate<TickStrategiesJob>(
        "tick-strategies",
        job => job.ExecuteAsync(CancellationToken.None),
        "* * * * *"); // Cron: Every minute
}

host.Run();
