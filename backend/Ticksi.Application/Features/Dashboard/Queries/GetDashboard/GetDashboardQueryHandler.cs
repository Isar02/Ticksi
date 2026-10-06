using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Events;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;
using Ticksi.Domain.Enums;

namespace Ticksi.Application.Features.Dashboard.Queries.GetDashboard;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private const int RecentOrderCount = 3;
    private const int TopEventCount = 3;

    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IEventClock _eventClock;

    public GetDashboardQueryHandler(IAppDbContext context, ICurrentUser currentUser, IEventClock eventClock)
    {
        _context = context;
        _currentUser = currentUser;
        _eventClock = eventClock;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var publicId = _currentUser.RequirePublicId();
        var user = await _context.AppUsers
            .AsNoTracking()
            .Where(u => u.PublicId == publicId && u.IsActive)
            .Select(u => new { u.Id, Role = u.Role!.Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedException("Your account could not be found.");

        var now = _eventClock.Now;
        var upcomingTickets = _context.Tickets
            .AsNoTracking()
            .Where(t => t.OrderItem!.Order!.AppUserId == user.Id
                && t.Status != TicketStatus.Cancelled
                && t.OrderItem.TicketType!.Event!.Date >= now);

        var dashboard = new DashboardDto
        {
            UpcomingTickets = await upcomingTickets.CountAsync(cancellationToken),
            NextEvent = await NextEventAsync(upcomingTickets, cancellationToken),
            Favorites = await _context.Favorites.CountAsync(f => f.AppUserId == user.Id, cancellationToken),
            RecentOrders = await RecentOrdersAsync(user.Id, cancellationToken)
        };

        if (user.Role is Role.Names.Admin or Role.Names.Organizer)
        {
            var editor = new EventEditor(user.Id, IsAdmin: user.Role == Role.Names.Admin);
            dashboard.Sales = await SalesAsync(editor, now, cancellationToken);
        }

        return dashboard;
    }

    private static async Task<DashboardEventDto?> NextEventAsync(IQueryable<Ticket> upcomingTickets, CancellationToken cancellationToken)
    {
        var next = await upcomingTickets
            .OrderBy(t => t.OrderItem!.TicketType!.Event!.Date)
            .Select(t => t.OrderItem!.TicketType!.Event!)
            .Select(e => new DashboardEventDto
            {
                EventId = e.PublicId,
                Name = e.Name,
                Date = e.Date,
                VenueName = e.Location!.Name,
                VenueCity = e.Location.City
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (next is not null)
            next.Tickets = await upcomingTickets.CountAsync(t => t.OrderItem!.TicketType!.Event!.PublicId == next.EventId, cancellationToken);

        return next;
    }

    private async Task<List<DashboardOrderDto>> RecentOrdersAsync(int userId, CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.AppUserId == userId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ThenByDescending(o => o.Id)
            .Take(RecentOrderCount)
            .Select(o => new DashboardOrderDto
            {
                OrderId = o.PublicId,
                EventNames = o.Items.OrderBy(i => i.Id).Select(i => i.TicketType!.Event!.Name).ToList(),
                Tickets = o.Items.Sum(i => i.Quantity),
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                CreatedAtUtc = o.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        // An order may hold several ticket types of one event; each event is named once, in order.
        orders.ForEach(o => o.EventNames = o.EventNames.Distinct().ToList());
        return orders;
    }

    private async Task<DashboardSalesDto> SalesAsync(EventEditor editor, DateTime now, CancellationToken cancellationToken)
    {
        var events = editor.Manageable(_context.Events.AsNoTracking());
        var eventIds = events.Select(e => e.Id);
        var paidItems = _context.OrderItems
            .AsNoTracking()
            .Where(i => i.Order!.Status == OrderStatus.Paid && eventIds.Contains(i.TicketType!.EventId));

        var topEvents = await paidItems
            .GroupBy(i => i.TicketType!.EventId)
            .Select(g => new { EventId = g.Key, TicketsSold = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.Quantity * i.UnitPrice) })
            .Join(_context.Events, s => s.EventId, e => e.Id, (s, e) => new DashboardTopEventDto
            {
                EventId = e.PublicId,
                Name = e.Name,
                Date = e.Date,
                TicketsSold = s.TicketsSold,
                Revenue = s.Revenue
            })
            .OrderByDescending(t => t.TicketsSold)
            .ThenByDescending(t => t.Revenue)
            .ThenBy(t => t.Name)
            .Take(TopEventCount)
            .ToListAsync(cancellationToken);

        return new DashboardSalesDto
        {
            AllEvents = editor.IsAdmin,
            UpcomingEvents = await events.CountAsync(e => e.Date >= now, cancellationToken),
            TicketsSold = await paidItems.SumAsync(i => i.Quantity, cancellationToken),
            Revenue = await paidItems.SumAsync(i => i.Quantity * i.UnitPrice, cancellationToken),
            ActiveUsers = editor.IsAdmin ? await _context.AppUsers.CountAsync(u => u.IsActive, cancellationToken) : null,
            TopEvents = topEvents
        };
    }
}
