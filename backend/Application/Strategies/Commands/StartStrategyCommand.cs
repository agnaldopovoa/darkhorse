using Darkhorse.Domain.Interfaces.Repositories;
using MediatR;

namespace Darkhorse.Application.Strategies.Commands;

public record StartStrategyCommand(Guid Id, Guid UserId) : IRequest;

public class StartStrategyCommandHandler(IStrategyRepository repo)
    : IRequestHandler<StartStrategyCommand>
{
    public async Task Handle(StartStrategyCommand request, CancellationToken cancellationToken)
    {
        var strategy = await repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new Exception("Strategy not found");

        if (strategy.UserId != request.UserId)
            throw new Exception("Unauthorized");

        strategy.Status = "running";
        
        // If circuit was open, we are manually resetting it by starting
        if (strategy.CircuitState == "OPEN")
        {
            strategy.CircuitState = "CLOSED";
            strategy.CircuitFailures = 0;
            // A real app would also reset the Polly Cache via CircuitBreakerFactory here
        }

        await repo.UpdateAsync(strategy, cancellationToken);
    }
}
