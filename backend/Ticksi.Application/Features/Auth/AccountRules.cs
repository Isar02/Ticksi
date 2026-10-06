using System.Text.RegularExpressions;
using FluentValidation;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Auth;

// The values are saved trimmed, so lengths and patterns are checked on the trimmed value too.
internal static partial class AccountRules
{
    private const int NameMinLength = 2;
    private const int PasswordMinLength = 6;

    public static IRuleBuilderOptions<T, string> PersonName<T>(this IRuleBuilderInitial<T, string> rule, string label, int maxLength) =>
        rule
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage($"{label} is required.")
            .Must(value => value.Trim().Length >= NameMinLength).WithMessage($"{label} must be at least {NameMinLength} characters.")
            .Must(value => value.Trim().Length <= maxLength).WithMessage($"{label} cannot exceed {maxLength} characters.");

    public static IRuleBuilderOptions<T, string> AccountEmail<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.")
            .MaximumLength(AppUser.Constraints.EmailMaxLength).WithMessage("Email cannot exceed {MaxLength} characters.");

    public static IRuleBuilderOptions<T, string> PhoneNumber<T>(this IRuleBuilderInitial<T, string> rule) =>
        rule
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Phone is required.")
            .Must(value => PhonePattern().IsMatch(value.Trim())).WithMessage("Please enter a valid phone number.")
            .Must(value => value.Trim().Length <= AppUser.Constraints.PhoneMaxLength)
            .WithMessage($"Phone cannot exceed {AppUser.Constraints.PhoneMaxLength} characters.");

    public static IRuleBuilderOptions<T, string> NewPassword<T>(this IRuleBuilderInitial<T, string> rule, string label) =>
        rule
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage($"{label} is required.")
            .MinimumLength(PasswordMinLength).WithMessage($"{label} must be at least {{MinLength}} characters.");

    [GeneratedRegex(@"^\+?[0-9\s-]{9,}$")]
    private static partial Regex PhonePattern();
}
