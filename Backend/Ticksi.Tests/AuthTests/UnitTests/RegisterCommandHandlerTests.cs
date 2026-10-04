using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Auth.Commands.Register;
using Ticksi.Application.Interfaces;
using Ticksi.Infrastructure.Data.Seeders;

namespace Ticksi.Tests.AuthTests.UnitTests;

public class RegisterCommandHandlerTests : AuthHandlerTestBase
{
    [Fact]
    public async Task Handle_NewEmail_CreatesUserWithUserRoleHashedPasswordAndSession()
    {
        var response = await RegisterAsync("marko@ticksi.com");

        await using var context = Database.CreateContext();
        var user = await context.AppUsers.Include(u => u.Role).SingleAsync(u => u.Email == "marko@ticksi.com");
        Assert.Equal(StaticDataSeeder.RoleNames.User, user.Role!.Name);
        Assert.Equal(user.PublicId.ToString(), response.PublicId);
        Assert.NotEqual(Password, user.PasswordHash);
        Assert.Equal(PasswordCheck.Valid, PasswordHasher.Verify(user, Password));

        var storedToken = Assert.Single(await RefreshTokensOfAsync(user));
        Assert.Equal(TokenService.HashRefreshToken(response.RefreshToken), storedToken.TokenHash);
    }

    [Fact]
    public async Task Handle_EmailTaken_ThrowsConflictAndAddsNoUser()
    {
        var existing = await AddUserAsync();

        await Assert.ThrowsAsync<ConflictException>(() => RegisterAsync(existing.Email));

        await using var context = Database.CreateContext();
        Assert.Equal(1, await context.AppUsers.CountAsync());
        Assert.Empty(await RefreshTokensOfAsync(existing));
    }

    private async Task<AuthResponseDto> RegisterAsync(string email)
    {
        await using var context = Database.CreateContext();
        var handler = new RegisterCommandHandler(context, TokenService, PasswordHasher);
        var command = new RegisterCommand
        {
            FirstName = "Marko",
            LastName = "Peric",
            Email = email,
            Password = Password,
            Phone = "061-123-456"
        };

        return await handler.Handle(command, CancellationToken.None);
    }
}
