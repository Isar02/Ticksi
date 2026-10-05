using FluentValidation;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Auth.Commands.Register;
using Ticksi.Application.Interfaces;

namespace Ticksi.Tests.AuthTests.UnitTests;

public class RegisterCommandHandlerTests : AuthHandlerTestBase
{
    [Fact]
    public async Task Handle_NewEmail_CreatesUserWithUserRoleHashedPasswordAndSession()
    {
        var response = await RegisterAsync("marko@ticksi.com");

        await using var context = Database.CreateContext();
        var user = await context.AppUsers.Include(u => u.Role).SingleAsync(u => u.Email == "marko@ticksi.com");
        Assert.Equal(Role.Names.User, user.Role!.Name);
        Assert.Equal(user.PublicId.ToString(), response.PublicId);
        Assert.NotEqual(Password, user.PasswordHash);
        Assert.Equal(PasswordCheck.Valid, PasswordHasher.Verify(user, Password));

        var storedToken = Assert.Single(await RefreshTokensOfAsync(user));
        Assert.Equal(TokenService.HashRefreshToken(response.RefreshToken), storedToken.TokenHash);
    }

    [Fact]
    public async Task Handle_EmailTaken_FailsOnEmailAndAddsNoUser()
    {
        var existing = await AddUserAsync();

        var error = await Assert.ThrowsAsync<ValidationException>(() => RegisterAsync(existing.Email));

        var failure = Assert.Single(error.Errors);
        Assert.Equal(nameof(RegisterCommand.Email), failure.PropertyName);
        Assert.Equal("An account with this email already exists.", failure.ErrorMessage);

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
