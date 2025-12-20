using MediatR;

namespace Ticksi.Application.Features.Reports.Queries.GetEventsByCategoryReport
{
    public class GetEventsByCategoryReportQuery : IRequest<byte[]>
    {
        public Guid CategoryPublicId { get; set; }
    }
}

