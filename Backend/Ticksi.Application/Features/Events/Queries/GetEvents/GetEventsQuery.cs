using MediatR;
using Ticksi.Application.Common;
using Ticksi.Application.DTOs;

namespace Ticksi.Application.Features.Events.Queries.GetEvents;

public class GetEventsQuery : IRequest<PagedResult<EventReadDto>>
{
    public string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
