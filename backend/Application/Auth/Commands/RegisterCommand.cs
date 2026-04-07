using FluentValidation;
using MediatR;
using Darkhorse.Domain.Entities;
using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Darkhorse.Application.Auth.Commands;

public record RegisterCommand(string Email, string Password) : IRequest<Guid>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(12);
    }
}

public class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordService passwordService,
    IEmailService emailService,
    IConfiguration config)
    : IRequestHandler<RegisterCommand, Guid>
{
    public async Task<Guid> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existing = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            throw new Exception("Email already registered.");

        var (hash, salt) = passwordService.HashPassword(request.Password);

        // Generate a cryptographically random 32-byte token, base64url-encoded for URL safety
        var rawTokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(rawTokenBytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('='); // base64url

        // Store only the SHA-256 hash — the plaintext never touches the DB
        var tokenHash = Convert.ToHexString(SHA256.HashData(rawTokenBytes)).ToLowerInvariant();

        var user = new User
        {
            Email = request.Email.ToLowerInvariant(),
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = false,
            EmailVerificationTokenHash = tokenHash,
            EmailVerificationTokenExpiry = DateTimeOffset.UtcNow.AddHours(24)
        };

        await userRepository.AddAsync(user, cancellationToken);

        var baseUrl = config["APP_BASE_URL"] ?? "https://localhost:5173";
        var activationUrl = $"{baseUrl}/verify-email?token={rawToken}";
        await emailService.SendVerificationEmailAsync(user.Email, activationUrl, cancellationToken);

        return user.Id;
    }
}
