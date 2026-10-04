using System.ComponentModel.DataAnnotations;

namespace Ticksi.Application.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required] public string Issuer { get; init; } = string.Empty;
    [Required] public string Audience { get; init; } = string.Empty;
    [Required, MinLength(32)] public string Key { get; init; } = string.Empty;
    [Range(1, 1440)] public int AccessTokenMinutes { get; init; } = 60;
    [Range(1, 90)] public int RefreshTokenDays { get; init; } = 7;
}
