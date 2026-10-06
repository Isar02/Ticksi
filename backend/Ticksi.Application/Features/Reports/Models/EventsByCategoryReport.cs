namespace Ticksi.Application.Features.Reports.Models;

public record EventsByCategoryReport(
    string CategoryName,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<EventsByCategoryReportRow> Events);
