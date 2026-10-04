using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Ticksi.Application.Interfaces;
using Ticksi.Application.Options;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider timeProvider) : IJwtTokenService
{
    private readonly JwtOptions _jwt = options.Value;

    public string CreateAccessToken(AppUser user)
    {
        var roleName = user.Role?.Name
            ?? throw new InvalidOperationException("The user's role must be loaded to issue a token.");

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.PublicId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim(ClaimTypes.Role, roleName),
            new Claim("RoleId", roleName)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: timeProvider.GetUtcNow().AddMinutes(_jwt.ExpirationMinutes).UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
