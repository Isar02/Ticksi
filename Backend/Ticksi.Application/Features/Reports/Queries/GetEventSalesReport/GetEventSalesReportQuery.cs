using MediatR;

namespace Ticksi.Application.Features.Reports.Queries.GetEventSalesReport;

public class GetEventSalesReportQuery : IRequest<byte[]>
{
    public Guid EventPublicId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
