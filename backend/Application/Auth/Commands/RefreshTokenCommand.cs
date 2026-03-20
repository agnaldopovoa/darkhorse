using MediatR;

namespace Darkhorse.Application.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResult>;

// The handler requires JWT regeneration logic same as LoginCommand and Redis interaction.
// Scaffolding a basic return for now.
public class RefreshTokenCommandHandler()
    : IRequestHandler<RefreshTokenCommand, LoginResult>
{
    public Task<LoginResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new Exception("Refresh token is required.");
            
        return Task.FromResult(new LoginResult("new-access-token", "new-refresh-token"));
    }
}
