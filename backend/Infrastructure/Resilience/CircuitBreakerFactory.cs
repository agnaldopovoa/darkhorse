using Darkhorse.Domain.Exceptions;
using Polly;
using Polly.CircuitBreaker;

namespace Darkhorse.Infrastructure.Resilience;

public class CircuitBreakerFactory
{
    // Real implementation would use Redis-backed distributed state
    // For now we use Polly's in-memory AsyncCircuitBreakerPolicy per strategy
    private readonly Dictionary<string, AsyncCircuitBreakerPolicy> _policies = [];

    public AsyncCircuitBreakerPolicy GetOrCreatePolicy(
        string strategyId, 
        int threshold = 3, 
        TimeSpan cooldown = default)
    {
        if (cooldown == default) cooldown = TimeSpan.FromMinutes(5);

        if (_policies.TryGetValue(strategyId, out var policy))
            return policy;

        policy = Policy
            .Handle<StrategyExecutionException>()
            .Or<Exception>(ex => ex.Message.Contains("Exchange"))
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: threshold,
                durationOfBreak: cooldown,
                onBreak: (exception, timespan) =>
                {
                    // Here we'd usually log to Serilog/Redis "Circuit OPEN"
                },
                onReset: () =>
                {
                    // "Circuit CLOSED"
                },
                onHalfOpen: () =>
                {
                    // "Circuit HALF-OPEN"
                }
            );

        _policies[strategyId] = policy;
        return policy;
    }
}
