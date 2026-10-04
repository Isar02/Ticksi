using Ticksi.Domain.Entities;

namespace Ticksi.Application.Interfaces;

public interface IJwtTokenService
{
    TokenPair IssueTokens(AppUser user);
    string HashRefreshToken(string refreshToken);
}

public sealed record TokenPair(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    string RefreshTokenHash,
    DateTime RefreshTokenExpiresAtUtc);
