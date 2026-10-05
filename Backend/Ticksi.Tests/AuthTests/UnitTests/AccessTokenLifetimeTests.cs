using System.IdentityModel.Tokens.Jwt;
using System.Text;
using API;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Ticksi.Application.Options;

namespace Ticksi.Tests.AuthTests.UnitTests;

public class AccessTokenLifetimeTests
{
    [Fact]
    public void Api_RejectsAnExpiredTokenWithoutTheDefaultFiveMinuteGracePeriod()
    {
        var jwt = new JwtOptions
        {
            Issuer = "Ticksi.Tests", Audience = "Ticksi.Tests",
            Key = "TestSigningKeyThatIsAtLeast32CharactersLong", AccessTokenMinutes = 15
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Options.Create(jwt));
        services.AddApi(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();
        var validation = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;
        var handler = new JwtSecurityTokenHandler();
        var now = DateTime.UtcNow;
        var signing = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)), SecurityAlgorithms.HmacSha256);
        string Token(DateTime expiry) => handler.WriteToken(new JwtSecurityToken(
            jwt.Issuer, jwt.Audience, notBefore: now.AddMinutes(-15), expires: expiry, signingCredentials: signing));

        handler.ValidateToken(Token(now.AddMinutes(1)), validation, out _);
        Assert.Throws<SecurityTokenExpiredException>(() => handler.ValidateToken(Token(now.AddSeconds(-1)), validation, out _));
    }
}
