using Darkhorse.Domain.Interfaces.Repositories;
using MediatR;
using System.Security.Cryptography;

namespace Darkhorse.Application.Auth.Commands;

public record VerifyEmailResult(bool Success, string Message);
public record VerifyEmailCommand(string Token) : IRequest<VerifyEmailResult>;

public class VerifyEmailCommandHandler(IUserRepository userRepository)
    : IRequestHandler<VerifyEmailCommand, VerifyEmailResult>
{
    public async Task<VerifyEmailResult> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Decode base64url token back to bytes, then compute SHA-256 for DB lookup
            var paddedToken = request.Token.Replace('-', '+').Replace('_', '/');
            var remainder = paddedToken.Length % 4;
            if (remainder != 0) paddedToken += new string('=', 4 - remainder);

            var tokenBytes = Convert.FromBase64String(paddedToken);
            var tokenHash = Convert.ToHexString(SHA256.HashData(tokenBytes)).ToLowerInvariant();

            var user = await userRepository.GetByVerificationTokenHashAsync(tokenHash, cancellationToken);
            if (user is null)
                return new VerifyEmailResult(false, "This link is invalid or has already been used.");

            if (user.IsActive)
                return new VerifyEmailResult(false, "This account is already active.");

            if (user.EmailVerificationTokenExpiry is null || user.EmailVerificationTokenExpiry < DateTimeOffset.UtcNow)
                return new VerifyEmailResult(false, "expired");

            // Activate and clear token fields (single-use)
            user.IsActive = true;
            user.EmailVerificationTokenHash = null;
            user.EmailVerificationTokenExpiry = null;
            await userRepository.UpdateAsync(user, cancellationToken);

            return new VerifyEmailResult(true, "Email verified successfully.");
        }
        catch
        {
            return new VerifyEmailResult(false, "This link is invalid or has already been used.");
        }
    }
}
