namespace Ticksi.Application.Features.Reports.Models;

public record EventsByCategoryReportRow(
    string Name,
    DateTime Date,
    string LocationName,
    string City,
    string Address,
    decimal? LowestPrice);
