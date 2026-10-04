using FluentValidation.TestHelper;
using Ticksi.Application.Features.Auth.Commands.Register;

namespace Ticksi.Tests.AuthTests.UnitTests;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Theory]
    [InlineData("+387 61 123 456")]
    [InlineData("061-123-456")]
    [InlineData("061123456")]
    public void Validate_ValidPhone_HasNoErrors(string phone)
    {
        var result = _validator.TestValidate(Command(c => c.Phone = phone));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("06112345")]
    [InlineData("++38761123456")]
    [InlineData("061-abc-456")]
    [InlineData("٠٦١١٢٣٤٥٦")]
    public void Validate_InvalidPhone_FailsOnPhone(string phone)
    {
        var result = _validator.TestValidate(Command(c => c.Phone = phone));

        result.ShouldHaveValidationErrorFor(x => x.Phone).WithErrorMessage("Please enter a valid phone number.");
    }

    [Fact]
    public void Validate_PasswordShorterThanSix_FailsOnPassword()
    {
        var result = _validator.TestValidate(Command(c => c.Password = "12345"));

        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 6 characters.");
    }

    [Theory]
    [InlineData("A")]
    [InlineData("")]
    public void Validate_TooShortFirstName_FailsOnFirstName(string firstName)
    {
        var result = _validator.TestValidate(Command(c => c.FirstName = firstName));

        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void Validate_MalformedEmail_FailsOnEmail()
    {
        var result = _validator.TestValidate(Command(c => c.Email = "ana.ticksi.com"));

        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage("Invalid email format.");
    }

    private static RegisterCommand Command(Action<RegisterCommand> change)
    {
        var command = new RegisterCommand
        {
            FirstName = "Ana",
            LastName = "Kovac",
            Email = "ana@ticksi.com",
            Password = "Secret123",
            Phone = "+387 61 123 456"
        };

        change(command);
        return command;
    }
}
