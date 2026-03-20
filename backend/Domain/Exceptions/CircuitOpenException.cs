namespace Darkhorse.Domain.Exceptions;

public class CircuitOpenException(string strategyId)
    : Exception($"Strategy {strategyId} is paused — circuit breaker is OPEN.");
