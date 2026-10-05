using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;

namespace Ticksi.Application.Features.Events.Queries.GetManagedEvents;

public class GetManagedEventsQueryHandler : IRequestHandler<GetManagedEventsQuery, PagedResult<ManagedEventDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public GetManagedEventsQueryHandler(IAppDbContext context, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResult<ManagedEventDto>> Handle(GetManagedEventsQuery request, CancellationToken cancellationToken)
    {
        var editor = await EventEditor.ResolveAsync(_context, _currentUser, cancellationToken);
        var events = editor.Manageable(_context.Events.AsNoTracking());

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var term = request.Name.Trim();
            events = events.Where(e => e.Name.Contains(term));
        }

        if (request.CategoryId.HasValue)
            events = events.Where(e => e.EventCategory!.PublicId == request.CategoryId.Value);

        if (request.LocationId.HasValue)
            events = events.Where(e => e.Location!.PublicId == request.LocationId.Value);

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

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (string.Equals(request.Period, "upcoming", StringComparison.OrdinalIgnoreCase))
            events = events.Where(e => e.Date >= now);
        else if (string.Equals(request.Period, "past", StringComparison.OrdinalIgnoreCase))
            events = events.Where(e => e.Date < now);

        var rows = events.Select(e => new ManagedEventDto
        {
            PublicId = e.PublicId,
            Name = e.Name,
            Date = e.Date,
            CategoryName = e.EventCategory!.Name,
            VenueName = e.Location!.Name,
            TicketsSold = e.TicketTypes
                .SelectMany(t => t.OrderItems)
                .Where(i => i.Order!.Status == OrderStatus.Paid)
                .Sum(i => (int?)i.Quantity) ?? 0,
            TicketsTotal = e.TicketTypes.Sum(t => (int?)t.Quantity) ?? 0
        });

        return await Sort(rows, request.SortBy, request.SortDescending)
            .ThenBy(e => e.PublicId)
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }

    private static IOrderedQueryable<ManagedEventDto> Sort(IQueryable<ManagedEventDto> rows, string? sortBy, bool descending) =>
        sortBy?.ToLowerInvariant() switch
        {
            "name" => descending ? rows.OrderByDescending(e => e.Name) : rows.OrderBy(e => e.Name),
            "venue" => descending ? rows.OrderByDescending(e => e.VenueName) : rows.OrderBy(e => e.VenueName),
            "sold" => descending ? rows.OrderByDescending(e => e.TicketsSold) : rows.OrderBy(e => e.TicketsSold),
            _ => descending ? rows.OrderByDescending(e => e.Date) : rows.OrderBy(e => e.Date)
        };
}
