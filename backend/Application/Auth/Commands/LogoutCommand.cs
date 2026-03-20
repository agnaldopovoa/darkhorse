using MediatR;
using Darkhorse.Domain.Interfaces.Repositories;

namespace Darkhorse.Application.Auth.Commands;

// Assuming Revocations are done via Redis logic (Revoked:{Jti}) inside the Web API or RedisCacheService natively.
public record LogoutCommand(string Jti) : IRequest;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    // A full implementation would inject RedisCacheService and add `jti` to the revoked list for 7 days.
    public Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
