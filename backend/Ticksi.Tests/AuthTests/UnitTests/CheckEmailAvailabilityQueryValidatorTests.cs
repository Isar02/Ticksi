using FluentValidation.TestHelper;
using Ticksi.Application.Features.Auth.Queries.CheckEmailAvailability;

namespace Ticksi.Tests.AuthTests.UnitTests;

public class CheckEmailAvailabilityQueryValidatorTests
{
    private readonly CheckEmailAvailabilityQueryValidator _validator = new();

    [Theory]
    [InlineData("ana@ticksi.com")]
    [InlineData("  ana@ticksi.com ")]
    public void Validate_ValidEmail_HasNoErrors(string email)
    {
        var result = _validator.TestValidate(new CheckEmailAvailabilityQuery(email));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null, "Email is required.")]
    [InlineData("", "Email is required.")]
    [InlineData("   ", "Email is required.")]
    [InlineData("ana.ticksi.com", "Invalid email format.")]
    [InlineData("ana@", "Invalid email format.")]
    public void Validate_InvalidEmail_FailsOnEmail(string? email, string message)
    {
        var result = _validator.TestValidate(new CheckEmailAvailabilityQuery(email));

        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorMessage(message);
    }

    [Fact]
    public void Validate_EmailOverTheLimitAfterTrimming_FailsOnEmail()
    {
        var local = new string('a', AppUser.Constraints.EmailMaxLength - "@ticksi.com".Length + 1);

        var result = _validator.TestValidate(new CheckEmailAvailabilityQuery($" {local}@ticksi.com "));

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage($"Email cannot exceed {AppUser.Constraints.EmailMaxLength} characters.");
    }
}
