using FluentValidation.TestHelper;
using Ticksi.Application.Features.Profile.Commands.ChangePassword;
using Ticksi.Application.Features.Profile.Commands.UpdateProfile;

namespace Ticksi.Tests.ProfileTests.UnitTests;

public class ProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _profileValidator = new();
    private readonly ChangePasswordCommandValidator _passwordValidator = new();

    [Fact]
    public void Validate_ValidDetails_HasNoErrors()
    {
        var result = _profileValidator.TestValidate(Details(_ => { }));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("06112345")]
    [InlineData("061-abc-456")]
    [InlineData("")]
    public void Validate_InvalidPhone_FailsOnPhone(string phone)
    {
        var result = _profileValidator.TestValidate(Details(c => c.Phone = phone));

        result.ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("")]
    public void Validate_TooShortNames_FailOnBothNames(string name)
    {
        var result = _profileValidator.TestValidate(Details(c => (c.FirstName, c.LastName) = (name, name)));

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public void Validate_TooLongLastName_FailsOnLastName()
    {
        var result = _profileValidator.TestValidate(Details(c => c.LastName = new string('a', AppUser.Constraints.LastNameMaxLength + 1)));

        result.ShouldHaveValidationErrorFor(x => x.LastName).WithErrorMessage("Last name cannot exceed 100 characters.");
    }

    [Fact]
    public void Validate_ValidPasswords_HasNoErrors()
    {
        var result = _passwordValidator.TestValidate(new ChangePasswordCommand { CurrentPassword = "Secret123", NewPassword = "Changed456" });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_MissingCurrentAndShortNewPassword_FailsOnBoth()
    {
        var result = _passwordValidator.TestValidate(new ChangePasswordCommand { CurrentPassword = "", NewPassword = "12345" });

        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword).WithErrorMessage("Current password is required.");
        result.ShouldHaveValidationErrorFor(x => x.NewPassword).WithErrorMessage("New password must be at least 6 characters.");
    }

    private static UpdateProfileCommand Details(Action<UpdateProfileCommand> change)
    {
        var command = new UpdateProfileCommand { FirstName = "Ana", LastName = "Kovac", Phone = "+387 61 123 456" };
        change(command);
        return command;
    }
}
