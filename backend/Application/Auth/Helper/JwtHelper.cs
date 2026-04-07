using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Darkhorse.Domain.Interfaces.Services;

namespace Darkhorse.Application.Auth.Helper;

public static class JwtHelper
{
    public static string GenerateJwtToken(Domain.Entities.User user, IConfiguration config)
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
            expires: DateTime.UtcNow.AddMinutes(double.Parse(config["JWT_EXPIRATION_MINUTES"] ?? "15")), // 15-minute access token max
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public static async void SaveRefreshToken(ICacheService cacheService, string refreshToken, string userId, int hours, CancellationToken cancellationToken)
    {
        await cacheService.SetAsync($"RefreshToken:{refreshToken}", userId, TimeSpan.FromHours(hours), cancellationToken);
    }

}
