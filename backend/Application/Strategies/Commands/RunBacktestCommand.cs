using MediatR;

namespace Darkhorse.Application.Strategies.Commands;

public record RunBacktestCommand(
    Guid Id,
    Guid UserId,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate) : IRequest<string>;

public class RunBacktestCommandHandler() : IRequestHandler<RunBacktestCommand, string>
{
    public Task<string> Handle(RunBacktestCommand request, CancellationToken cancellationToken)
    {
        // Enqueue to Hangfire: BackgroundJob.Enqueue<IRunBacktestJob>(x => x.ExecuteAsync(request.Id, ...));
        var dummyJobId = Guid.NewGuid().ToString();
        return Task.FromResult(dummyJobId);
    }
}
