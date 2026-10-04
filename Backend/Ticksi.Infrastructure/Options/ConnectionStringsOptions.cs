using System.ComponentModel.DataAnnotations;

namespace Ticksi.Infrastructure.Options;

public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";

    [Required] public string DefaultConnection { get; init; } = string.Empty;
}
