using Darkhorse.Domain.Entities;
using Darkhorse.Domain.Interfaces.Repositories;
using MediatR;

namespace Darkhorse.Application.Strategies.Commands;

public record UpdateStrategyCommand(
    Guid Id,
    Guid UserId,
    string Script,
    string Parameters) : IRequest;

public class UpdateStrategyCommandHandler(IStrategyRepository repo)
    : IRequestHandler<UpdateStrategyCommand>
{
    public async Task Handle(UpdateStrategyCommand request, CancellationToken cancellationToken)
    {
        var strategy = await repo.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new Exception("Strategy not found");

        if (strategy.UserId != request.UserId)
            throw new Exception("Unauthorized");

        strategy.Script = request.Script;
        strategy.Parameters = string.IsNullOrWhiteSpace(request.Parameters) ? "{}" : request.Parameters;
        strategy.ScriptVersion++;
        
        var newVersion = new StrategyVersion
        {
            Version = strategy.ScriptVersion,
            Script = strategy.Script,
            Parameters = strategy.Parameters
        };
        
        strategy.Versions.Add(newVersion);
        
        await repo.UpdateAsync(strategy, cancellationToken);
    }
}
