using MediatR;
using Darkhorse.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace Darkhorse.Application.Auth.Commands;

public record LogoutCommand(string? Jti, string? RefreshToken = null) : IRequest;

public class LogoutCommandHandler(
    ICacheService cacheService,
    IConfiguration config) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Jti))
        {
            var cacheKeyJti = $"Revoked:{request.Jti}";
            await cacheService.SetAsync(cacheKeyJti, true, TimeSpan.FromDays(int.Parse(config["JTI_BLACKLIST_EXPIRATION_DAYS"] ?? "7")), cancellationToken);
        }

        if (!string.IsNullOrEmpty(request.RefreshToken))
        {
            var cacheKeyRefresh = $"Revoked:{request.RefreshToken}";
            await cacheService.SetAsync(cacheKeyRefresh, true, TimeSpan.FromDays(int.Parse(config["REFRESHTOKEN_BLACKLIST_EXPIRATION_DAYS"] ?? "30")), cancellationToken);
        }
    }
}
