using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Auth;

internal static class AuthSession
{
    public static AuthResponseDto Start(IAppDbContext context, IJwtTokenService tokenService, AppUser user)
    {
        var tokens = tokenService.IssueTokens(user);

        context.RefreshTokens.Add(new RefreshToken
        {
            AppUser = user,
            TokenHash = tokens.RefreshTokenHash,
            ExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc
        });

        return new AuthResponseDto
        {
            AccessToken = tokens.AccessToken,
            AccessTokenExpiresAtUtc = tokens.AccessTokenExpiresAtUtc,
            RefreshToken = tokens.RefreshToken,
            RefreshTokenExpiresAtUtc = tokens.RefreshTokenExpiresAtUtc,
            Email = user.Email,
            PublicId = user.PublicId.ToString(),
            FirstName = user.FirstName
        };
    }
}
