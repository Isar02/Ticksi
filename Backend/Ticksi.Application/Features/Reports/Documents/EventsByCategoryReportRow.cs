namespace Ticksi.Application.Features.Reports.Documents;

public record EventsByCategoryReportRow(
    string Name,
    DateTime Date,
    string LocationName,
    string City,
    string Address,
    decimal? LowestPrice);
