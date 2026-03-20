using Darkhorse.Domain.Interfaces.Repositories;
using MediatR;

namespace Darkhorse.Application.Auth.Commands;

public record VerifyEmailCommand(string Token) : IRequest<bool>;

public class VerifyEmailCommandHandler(IUserRepository userRepository) 
    : IRequestHandler<VerifyEmailCommand, bool>
{
    public async Task<bool> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        // Simple token decoding for scaffolding (assuming Token is base64 encoded email for now)
        try
        {
            var emailBytes = Convert.FromBase64String(request.Token);
            var email = System.Text.Encoding.UTF8.GetString(emailBytes);
            
            var user = await userRepository.GetByEmailAsync(email, cancellationToken);
            if (user is null || user.IsActive) return false;

            user.IsActive = true;
            await userRepository.UpdateAsync(user, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
