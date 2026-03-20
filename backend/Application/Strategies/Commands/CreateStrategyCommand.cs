using Darkhorse.Domain.Entities;
using Darkhorse.Domain.Interfaces.Repositories;
using MediatR;

namespace Darkhorse.Application.Strategies.Commands;

public record CreateStrategyCommand(
    Guid UserId,
    Guid? CredentialId,
    string Name,
    string Symbol,
    string Timeframe,
    string Script,
    string Parameters,
    string Mode,
    decimal? MaxPositionSize,
    decimal? MaxDailyVolume) : IRequest<Guid>;

public class CreateStrategyCommandHandler(IStrategyRepository repo)
    : IRequestHandler<CreateStrategyCommand, Guid>
{
    public async Task<Guid> Handle(CreateStrategyCommand request, CancellationToken cancellationToken)
    {
        var strategy = new Strategy
        {
            UserId = request.UserId,
            CredentialId = request.CredentialId,
            Name = request.Name,
            Symbol = request.Symbol,
            Timeframe = request.Timeframe,
            Script = request.Script,
            Parameters = string.IsNullOrWhiteSpace(request.Parameters) ? "{}" : request.Parameters,
            Mode = request.Mode,
            MaxPositionSize = request.MaxPositionSize,
            MaxDailyVolume = request.MaxDailyVolume,
            ScriptVersion = 1,
            Versions = [
                new StrategyVersion { Version = 1, Script = request.Script, Parameters = request.Parameters }
            ]
        };

        await repo.AddAsync(strategy, cancellationToken);
        return strategy.Id;
    }
}
