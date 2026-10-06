using Ticksi.Application.Features.Users.Commands.CreateUser;
using Ticksi.Application.Features.Users.Commands.UpdateUser;

namespace Ticksi.Tests.UserTests.UnitTests;

public class UserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _create = new();
    private readonly UpdateUserCommandValidator _update = new();

    [Fact]
    public void Validate_CompleteAccount_Passes()
    {
        Assert.True(_create.Validate(ValidCreate()).IsValid);
    }

    [Fact]
    public void Validate_EveryFieldWrong_ReportsEachOne()
    {
        var command = new CreateUserCommand
        {
            FirstName = "   ",
            LastName = " A ",
            Email = "not-an-email",
            Phone = "12ab",
            RoleId = Guid.Empty,
            Password = "12345"
        };

        var errors = _create.Validate(command).Errors.ToDictionary(e => e.PropertyName, e => e.ErrorMessage);

        Assert.Equal("First name is required.", errors["FirstName"]);
        Assert.Equal("Last name must be at least 2 characters.", errors["LastName"]);
        Assert.Equal("Invalid email format.", errors["Email"]);
        Assert.Equal("Please enter a valid phone number.", errors["Phone"]);
        Assert.Equal("Role is required.", errors["RoleId"]);
        Assert.Equal("Password must be at least 6 characters.", errors["Password"]);
    }

    [Fact]
    public void Validate_MissingNames_DoNotThrow()
    {
        var command = ValidCreate();
        command.FirstName = null!;
        command.LastName = null!;

        var result = _create.Validate(command);

        Assert.Equal(["FirstName", "LastName"], result.Errors.Select(e => e.PropertyName));
    }

    [Fact]
    public void Validate_UpdateWithoutUser_IsRejectedAndNeedsNoPassword()
    {
        var command = new UpdateUserCommand
        {
            FirstName = "Amar",
            LastName = "Hadzic",
            Email = "amar@ticksi.com",
            Phone = "+387 62 555 444",
            RoleId = Guid.NewGuid()
        };

        var result = _update.Validate(command);

        Assert.Equal("PublicId", Assert.Single(result.Errors).PropertyName);
    }

    private static CreateUserCommand ValidCreate() => new()
    {
        FirstName = "Amar",
        LastName = "Hadzic",
        Email = "amar@ticksi.com",
        Phone = "+387 62 555 444",
        RoleId = Guid.NewGuid(),
        Password = "Secret123"
    };
}
