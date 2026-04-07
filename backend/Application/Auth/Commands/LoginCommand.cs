using Darkhorse.Application.Auth.Helper;
using Darkhorse.Domain.Exceptions;
using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Domain.Interfaces.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Darkhorse.Application.Auth.Commands;

public record LoginResult(string AccessToken, string RefreshToken);
public record LoginCommand(string Email, string Password) : IRequest<LoginResult>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordService passwordService,
    IConfiguration config,
    ICacheService cacheService)
    : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !passwordService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            throw new InvalidCredentialsException();

        if (!user.IsActive)
            throw new Exception("Email not verified. Please check your inbox and click the activation link.");

        var accessToken = JwtHelper.GenerateJwtToken(user, config);
        var refreshToken = JwtHelper.GenerateRefreshToken();

        // Store new refresh token
        JwtHelper.SaveRefreshToken(
            cacheService,
            refreshToken,
            user.Id.ToString(),
            int.Parse(config["REFRESHTOKEN_EXPIRATION_HOURS"] ?? "12"),
            cancellationToken);

        return new LoginResult(accessToken, refreshToken);
    }
}
