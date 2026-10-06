using System.Globalization;

namespace Ticksi.Infrastructure.Reports;

public static class ReportFormat
{
    public static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("bs-Latn-BA");

    public static string Date(DateOnly date) => date.ToString("d", Culture);

    public static string DateTime(DateTime value) => value.ToString("g", Culture);

    public static string Count(int value) => value.ToString("N0", Culture);

    public static string Price(decimal? price) => price is { } value ? value.ToString("C", Culture) : "-";

    public static string Period(DateOnly? from, DateOnly? to) => (from, to) switch
    {
        ({ } start, { } end) => $"{Date(start)} – {Date(end)}",
        ({ } start, null) => $"From {Date(start)}",
        (null, { } end) => $"Until {Date(end)}",
        _ => "All dates"
    };
}
