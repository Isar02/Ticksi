using MediatR;

namespace Ticksi.Application.Features.Reports.Queries.GetEventsByCategoryReport;

public class GetEventsByCategoryReportQuery : IRequest<byte[]>
{
    public Guid CategoryPublicId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
