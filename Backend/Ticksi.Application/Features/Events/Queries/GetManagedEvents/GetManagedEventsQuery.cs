using MediatR;
using Ticksi.Application.Common;

namespace Ticksi.Application.Features.Events.Queries.GetManagedEvents;

public class GetManagedEventsQuery : IRequest<PagedResult<ManagedEventDto>>
{
    public string? Name { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? LocationId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    public string? Period { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
