using Darkhorse.Domain.Exceptions;
using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Domain.Interfaces.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

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
    IConfiguration config) 
    : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !passwordService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
            throw new InvalidCredentialsException();

        if (!user.IsActive)
            throw new Exception("Account not verified.");

        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        return new LoginResult(accessToken, refreshToken);
    }

    private string GenerateJwtToken(Darkhorse.Domain.Entities.User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT_SECRET"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15), // 15-minute access token max
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
