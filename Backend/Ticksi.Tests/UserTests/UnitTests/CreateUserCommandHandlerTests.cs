using FluentValidation;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Users;
using Ticksi.Application.Features.Users.Commands.CreateUser;
using Ticksi.Application.Interfaces;

namespace Ticksi.Tests.UserTests.UnitTests;

public class CreateUserCommandHandlerTests : UserHandlerTestBase
{
    [Fact]
    public async Task Handle_Admin_CreatesTheAccountWithItsRoleAndAHashedPassword()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var command = Command();

        var result = await CreateAsync(admin, command);

        var stored = (await FindUserAsync(result.PublicId))!;
        Assert.Equal("Amar", stored.FirstName);
        Assert.Equal("Hadzic", stored.LastName);
        Assert.Equal(command.Email.Trim(), stored.Email);
        Assert.Equal(Role.Names.Organizer, stored.Role!.Name);
        Assert.Equal(Now, stored.RegistrationDate);
        Assert.True(stored.IsActive);
        Assert.Equal(PasswordCheck.Valid, PasswordHasher.Verify(stored, "Secret123"));
        Assert.Equal(Role.Names.Organizer, result.RoleName);
    }

    [Fact]
    public async Task Handle_EmailAlreadyUsed_ThrowsAValidationErrorOnTheEmail()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var command = Command();
        command.Email = admin.Email;

        var exception = await Assert.ThrowsAsync<ValidationException>(() => CreateAsync(admin, command));

        var error = Assert.Single(exception.Errors);
        Assert.Equal("Email", error.PropertyName);
        Assert.Equal("An account with this email already exists.", error.ErrorMessage);
    }

    [Fact]
    public async Task Handle_UnknownRole_ThrowsAValidationErrorOnTheRole()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var command = Command();
        command.RoleId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<ValidationException>(() => CreateAsync(admin, command));

        Assert.Equal("RoleId", Assert.Single(exception.Errors).PropertyName);
    }

    [Fact]
    public async Task Handle_Organizer_ThrowsForbidden()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);

        await Assert.ThrowsAsync<ForbiddenException>(() => CreateAsync(organizer, Command()));
    }

    [Fact]
    public async Task Handle_DeactivatedAdmin_ThrowsUnauthorized()
    {
        var admin = await AddUserAsync(Role.Names.Admin, isActive: false);

        await Assert.ThrowsAsync<UnauthorizedException>(() => CreateAsync(admin, Command()));
    }

    private static CreateUserCommand Command()
    {
        var command = UserInputOf<CreateUserCommand>();
        command.Password = "Secret123";
        return command;
    }

    private async Task<UserDto> CreateAsync(AppUser caller, CreateUserCommand command)
    {
        await using var context = Database.CreateContext();
        var handler = new CreateUserCommandHandler(context, SignedIn(caller), PasswordHasher, Clock);
        return await handler.Handle(command, CancellationToken.None);
    }
}
