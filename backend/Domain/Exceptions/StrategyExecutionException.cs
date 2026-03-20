namespace Darkhorse.Domain.Exceptions;

public class StrategyExecutionException(string message, Exception? inner = null)
    : Exception(message, inner);
