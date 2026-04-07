using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Darkhorse.Application.Auth.Helper;
using Darkhorse.Domain.Interfaces.Repositories;
using Darkhorse.Domain.Interfaces.Services;
using Darkhorse.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Darkhorse.Application.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResult>;

public class RefreshTokenCommandHandler(
    ICacheService cacheService,
    IUserRepository userRepository,
    IConfiguration config)
    : IRequestHandler<RefreshTokenCommand, LoginResult>
{
    public async Task<LoginResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new Exception("Refresh token is required.");

        var userIdString = await cacheService.GetAsync<string>($"RefreshToken:{request.RefreshToken}", cancellationToken);
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            throw new Exception("Invalid or expired refresh token.");

        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
            throw new Exception("User not found or inactive.");

        // Revoke the old refresh token
        await cacheService.RemoveAsync($"RefreshToken:{request.RefreshToken}", cancellationToken);

        var accessToken = JwtHelper.GenerateJwtToken(user, config);
        var newRefreshToken = JwtHelper.GenerateRefreshToken();

        // Store new refresh token
        JwtHelper.SaveRefreshToken(
            cacheService,
            newRefreshToken,
            user.Id.ToString(),
            int.Parse(config["REFRESHTOKEN_EXPIRATION_HOURS"] ?? "12"),
            cancellationToken);

        return new LoginResult(accessToken, newRefreshToken);
    }
}
