namespace Ticksi.Infrastructure.Options;

public sealed class SeedingOptions
{
    public const string SectionName = "Seeding";

    public bool DemoData { get; init; }
    public string DemoPassword { get; init; } = string.Empty;
}
