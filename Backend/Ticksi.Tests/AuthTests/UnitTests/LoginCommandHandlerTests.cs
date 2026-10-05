using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Ticksi.Application.Common;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Auth.Commands.Login;
using Ticksi.Application.Interfaces;

namespace Ticksi.Tests.AuthTests.UnitTests;

public class LoginCommandHandlerTests : AuthHandlerTestBase
{
    [Fact]
    public async Task Handle_ValidCredentials_IssuesTokensAndStoresOnlyRefreshTokenHash()
    {
        var user = await AddUserAsync();

        var response = await LoginAsync(user.Email, Password);

        var claims = new JwtSecurityTokenHandler().ReadJwtToken(response.AccessToken).Claims.ToList();
        Assert.Equal(user.PublicId.ToString(), claims.Single(c => c.Type == AuthClaims.PublicId).Value);
        Assert.Equal(Role.Names.User, claims.Single(c => c.Type == AuthClaims.Role).Value);

        var storedToken = Assert.Single(await RefreshTokensOfAsync(user));
        Assert.Equal(TokenService.HashRefreshToken(response.RefreshToken), storedToken.TokenHash);
        Assert.NotEqual(response.RefreshToken, storedToken.TokenHash);
        Assert.Equal(Now.AddDays(7), storedToken.ExpiresAtUtc);
    }

    [Theory]
    [InlineData("ana@ticksi.com", "WrongPassword1")]
    [InlineData("nobody@ticksi.com", Password)]
    public async Task Handle_InvalidCredentials_ThrowsUnauthorizedWithTheSameMessage(string email, string password)
    {
        var user = await AddUserAsync();

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(email, password));

        Assert.Equal("Invalid email or password.", exception.Message);
        Assert.Empty(await RefreshTokensOfAsync(user));
    }

    [Fact]
    public async Task Handle_LegacySha256Hash_SignsInAndUpgradesTheHash()
    {
        var user = await AddUserAsync();
        var legacyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(Password)));
        await using (var context = Database.CreateContext())
        {
            var stored = await context.AppUsers.SingleAsync(u => u.Id == user.Id);
            stored.PasswordHash = legacyHash;
            await context.SaveChangesAsync();
        }

        await LoginAsync(user.Email, Password);

        await using var assertContext = Database.CreateContext();
        var upgraded = await assertContext.AppUsers.SingleAsync(u => u.Id == user.Id);
        Assert.NotEqual(legacyHash, upgraded.PasswordHash);
        Assert.Equal(PasswordCheck.Valid, PasswordHasher.Verify(upgraded, Password));
    }

    [Fact]
    public async Task Handle_DeactivatedAccount_ThrowsUnauthorizedAndStartsNoSession()
    {
        var user = await AddUserAsync();
        await DeactivateAsync(user);

        var exception = await Assert.ThrowsAsync<UnauthorizedException>(() => LoginAsync(user.Email, Password));

        Assert.Equal("This account has been deactivated.", exception.Message);
        Assert.Empty(await RefreshTokensOfAsync(user));
    }

    private async Task<AuthResponseDto> LoginAsync(string email, string password)
    {
        await using var context = Database.CreateContext();
        var handler = new LoginCommandHandler(context, TokenService, PasswordHasher);
        return await handler.Handle(new LoginCommand { Email = email, Password = password }, CancellationToken.None);
    }
}
