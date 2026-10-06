using FluentValidation;

namespace Ticksi.Application.Features.Auth;

internal static class AccountRules
{
    private const int NameMinLength = 2;
    private const int PasswordMinLength = 6;
    private const string PhonePattern = @"^\+?[0-9\s-]{9,}$";

    public static IRuleBuilderOptions<T, string> PersonName<T>(this IRuleBuilder<T, string> rule, string label, int maxLength) =>
        rule
            .NotEmpty().WithMessage($"{label} is required.")
            .MinimumLength(NameMinLength).WithMessage($"{label} must be at least {{MinLength}} characters.")
            .MaximumLength(maxLength).WithMessage($"{label} cannot exceed {{MaxLength}} characters.");

    public static IRuleBuilderOptions<T, string> PhoneNumber<T>(this IRuleBuilder<T, string> rule, int maxLength) =>
        rule
            .NotEmpty().WithMessage("Phone is required.")
            .Matches(PhonePattern).WithMessage("Please enter a valid phone number.")
            .MaximumLength(maxLength).WithMessage("Phone cannot exceed {MaxLength} characters.");

    public static IRuleBuilderOptions<T, string> NewPassword<T>(this IRuleBuilder<T, string> rule, string label) =>
        rule
            .NotEmpty().WithMessage($"{label} is required.")
            .MinimumLength(PasswordMinLength).WithMessage($"{label} must be at least {{MinLength}} characters.");
}
