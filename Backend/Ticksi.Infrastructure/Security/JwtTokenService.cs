using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Ticksi.Application.Common;
using Ticksi.Application.Interfaces;
using Ticksi.Application.Options;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : IJwtTokenService
{
    private const int RefreshTokenBytes = 64;

    private readonly JwtOptions _jwt = options.Value;

    public TokenPair IssueTokens(AppUser user)
    {
        var now = timeProvider.GetUtcNow();
        var accessExpiresAtUtc = now.AddMinutes(_jwt.AccessTokenMinutes).UtcDateTime;
        var refreshToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));

        return new TokenPair(
            CreateAccessToken(user, accessExpiresAtUtc),
            accessExpiresAtUtc,
            refreshToken,
            HashRefreshToken(refreshToken),
            now.AddDays(_jwt.RefreshTokenDays).UtcDateTime);
    }

    public string HashRefreshToken(string refreshToken) =>
        Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    private string CreateAccessToken(AppUser user, DateTime expiresAtUtc)
    {
        var roleName = user.Role?.Name
            ?? throw new InvalidOperationException("The user's role must be loaded to issue a token.");

        var claims = new[]
        {
            new Claim(AuthClaims.PublicId, user.PublicId.ToString()),
            new Claim(AuthClaims.Email, user.Email),
            new Claim(AuthClaims.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(AuthClaims.Role, roleName)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
