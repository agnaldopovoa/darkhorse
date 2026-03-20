using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Domain.Interfaces.Services;
using MediatR;

namespace Darkhorse.Application.Auth.Commands;

public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : IRequest;

public class ChangePasswordCommandHandler(
    IUserRepository userRepository, 
    IPasswordService passwordService) 
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new Exception("User not found.");

        if (!passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
            throw new Exception("Invalid current password.");

        var (hash, salt) = passwordService.HashPassword(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;

        await userRepository.UpdateAsync(user, cancellationToken);

        // A full implementation would enqueue job to revoke all active refresh tokens for User here.
    }
}
