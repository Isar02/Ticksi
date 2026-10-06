using System.ComponentModel.DataAnnotations;

namespace Ticksi.Application.Options;

public sealed class EventOptions : IValidatableObject
{
    public const string SectionName = "Events";

    [Required] public string TimeZone { get; init; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrWhiteSpace(TimeZone) && !TimeZoneInfo.TryFindSystemTimeZoneById(TimeZone, out _))
            yield return new ValidationResult($"The time zone \"{TimeZone}\" is not known.", [nameof(TimeZone)]);
    }
}
