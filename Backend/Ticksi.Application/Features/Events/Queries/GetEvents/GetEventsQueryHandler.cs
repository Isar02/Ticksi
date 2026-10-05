using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Events.Queries.GetEvents;

public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, PagedResult<EventReadDto>>
{
    private readonly IAppDbContext _context;

    public GetEventsQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<EventReadDto>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var events = _context.Events.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            events = events.Where(e => e.Name.Contains(term) || e.Description.Contains(term));
        }

        if (request.CategoryId.HasValue)
            events = events.Where(e => e.EventCategory!.PublicId == request.CategoryId.Value);

        if (request.LocationId.HasValue)
            events = events.Where(e => e.Location!.PublicId == request.LocationId.Value);

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim();
            events = events.Where(e => e.Location!.City == city);
        }

        if (request.DateFrom is { } dateFrom)
        {
            var from = dateFrom.ToDateTime(TimeOnly.MinValue);
            events = events.Where(e => e.Date >= from);
        }

        if (request.DateTo is { } dateTo)
        {
            var to = dateTo.ToDateTime(TimeOnly.MaxValue);
            events = events.Where(e => e.Date <= to);
        }

        if (request.MinPrice.HasValue)
            events = events.Where(e => e.TicketTypes.Min(t => (decimal?)t.Price) >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            events = events.Where(e => e.TicketTypes.Min(t => (decimal?)t.Price) <= request.MaxPrice.Value);

        return await Sort(events, request.SortBy, request.SortDescending)
            .ThenBy(e => e.Id)
            .Select(EventProjections.ToReadDto)
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }

    private static IOrderedQueryable<Event> Sort(IQueryable<Event> events, string? sortBy, bool descending) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name" => descending ? events.OrderByDescending(e => e.Name) : events.OrderBy(e => e.Name),
            "price" => descending
                ? events.OrderByDescending(e => e.TicketTypes.Min(t => (decimal?)t.Price))
                : events.OrderBy(e => e.TicketTypes.Min(t => (decimal?)t.Price)),
            _ => descending ? events.OrderByDescending(e => e.Date) : events.OrderBy(e => e.Date)
        };
}
