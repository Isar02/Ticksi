using System.ComponentModel.DataAnnotations;

namespace Ticksi.Application.Options;

public sealed class JwtOptions : IValidatableObject
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; init; } = string.Empty;
    [Required] public string Audience { get; init; } = string.Empty;
    [Required, MinLength(32)] public string Key { get; init; } = string.Empty;
    [Range(1, 1440)] public int AccessTokenMinutes { get; init; } = 15;
    [Range(5, 1440)] public int SessionIdleMinutes { get; init; } = 30;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AccessTokenMinutes >= SessionIdleMinutes)
        {
            yield return new ValidationResult(
                "The access token must expire before the session's idle window ends.",
                [nameof(AccessTokenMinutes), nameof(SessionIdleMinutes)]);
        }
    }
}
