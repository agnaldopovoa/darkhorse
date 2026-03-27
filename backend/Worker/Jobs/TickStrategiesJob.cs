using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Domain.Interfaces.Services;
using MediatR;
using Serilog;

namespace Darkhorse.Worker.Jobs;

public class TickStrategiesJob(
    IStrategyRepository strategyRepo/*,
    IStrategyRunner runner,
    IBrokerService brokerService,
    IMediator mediator*/) // Used if we dispatch commands internally
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        Log.Information("TickStrategiesJob started");

        var runningStrategies = await strategyRepo.GetAllRunningAsync(ct);
        if (!runningStrategies.Any())
        {
            Log.Debug("No running strategies found.");
            return;
        }

        foreach (var strategy in runningStrategies)
        {
            try
            {
                // 1. Resolve strategy parameters, timeframe, credential
                // 2. Fetch recent OHLCV data using broker adapter
                // 3. Check circuit breaker status
                // 4. Create StrategyContext
                // 5. Build context snapshot for Execution log
                // 6. Invoke sandboxed python runner

                Log.Information($"Ticked strategy {strategy.Id}");
                /*
                var output = await runner.RunAsync(strategy.Script, context, ct);
                if (output.Signal == "BUY") { PlaceOrder(...) }
                */
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to tick strategy {strategy.Id}");
                strategy.CircuitFailures++;
                if (strategy.CircuitFailures >= 3)
                {
                    strategy.CircuitState = "OPEN";
                    Log.Warning($"Circuit OPENED for strategy {strategy.Id}");
                }
                await strategyRepo.UpdateAsync(strategy, ct);
            }
        }
    }
}
