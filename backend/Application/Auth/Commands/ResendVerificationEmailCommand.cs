using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Darkhorse.Application.Auth.Commands;

public record ResendVerificationEmailCommand(string Email) : IRequest<bool>;

public class ResendVerificationEmailCommandHandler(
    IUserRepository userRepository,
    IEmailService emailService,
    IConfiguration config)
    : IRequestHandler<ResendVerificationEmailCommand, bool>
{
    public async Task<bool> Handle(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Obscure: always return true to prevent email enumeration
        if (user is null || user.IsActive)
            return true;

        // Generate a fresh token
        var rawTokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(rawTokenBytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var tokenHash = Convert.ToHexString(SHA256.HashData(rawTokenBytes)).ToLowerInvariant();

        user.EmailVerificationTokenHash = tokenHash;
        user.EmailVerificationTokenExpiry = DateTimeOffset.UtcNow.AddHours(24);

        await userRepository.UpdateAsync(user, cancellationToken);

        var baseUrl = config["APP_BASE_URL"] ?? "https://localhost:5173";
        var activationUrl = $"{baseUrl}/verify-email?token={rawToken}";
        await emailService.SendVerificationEmailAsync(user.Email, activationUrl, cancellationToken);

        return true;
    }
}
