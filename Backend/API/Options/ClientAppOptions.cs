using System.ComponentModel.DataAnnotations;

namespace API.Options;

public sealed class ClientAppOptions
{
    public const string SectionName = "ClientApp";
    public const string CorsPolicy = "ClientApp";

    [Required, MinLength(1)] public string[] AllowedOrigins { get; init; } = [];
}
