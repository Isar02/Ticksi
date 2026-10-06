using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Events;
using Ticksi.Application.Features.Reports.Models;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;

namespace Ticksi.Application.Features.Reports.Queries.GetEventSalesReport;

public class GetEventSalesReportQueryHandler : IRequestHandler<GetEventSalesReportQuery, byte[]>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IReportRenderer _renderer;
    private readonly TimeProvider _timeProvider;

    public GetEventSalesReportQueryHandler(
        IAppDbContext context,
        ICurrentUser currentUser,
        IReportRenderer renderer,
        TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _renderer = renderer;
        _timeProvider = timeProvider;
    }

    public async Task<byte[]> Handle(GetEventSalesReportQuery request, CancellationToken cancellationToken)
    {
        var editor = await EventEditor.ResolveAsync(_context, _currentUser, cancellationToken);

        var found = await _context.Events
            .AsNoTracking()
            .Where(e => e.PublicId == request.EventPublicId)
            .Select(e => new
            {
                e.Id,
                OwnerId = e.AppUserId,
                e.Name,
                e.Date,
                VenueName = e.Location!.Name,
                VenueCity = e.Location.City,
                TicketTypes = e.TicketTypes.OrderBy(t => t.Id).Select(t => new { t.Id, t.Name }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Event not found.");

        editor.EnsureCanManage(found.OwnerId);

        var zone = _timeProvider.LocalTimeZone;
        var paidItems = _context.OrderItems
            .AsNoTracking()
            .Where(i => i.TicketType!.EventId == found.Id && i.Order!.Status == OrderStatus.Paid);

        // The period is in whole local days, while orders are paid at UTC times.
        if (request.DateFrom is { } dateFrom)
        {
            var fromUtc = StartOfDayUtc(dateFrom, zone);
            paidItems = paidItems.Where(i => i.Order!.PaidAtUtc >= fromUtc);
        }

        if (request.DateTo is { } dateTo)
        {
            var toUtc = TimeZoneInfo.ConvertTimeToUtc(dateTo.ToDateTime(TimeOnly.MaxValue), zone);
            paidItems = paidItems.Where(i => i.Order!.PaidAtUtc <= toUtc);
        }

        var sales = await paidItems
            .Select(i => new
            {
                i.TicketTypeId,
                PaidAtUtc = i.Order!.PaidAtUtc!.Value,
                i.Quantity,
                Revenue = i.Quantity * i.UnitPrice
            })
            .ToListAsync(cancellationToken);

        var byTicketType = sales.ToLookup(s => s.TicketTypeId);
        var ticketTypes = found.TicketTypes
            .Select(t => new TicketTypeSales(
                t.Name,
                byTicketType[t.Id].Sum(s => s.Quantity),
                byTicketType[t.Id].Sum(s => s.Revenue)))
            .ToList();

        var days = sales
            .GroupBy(s => LocalDate(s.PaidAtUtc, zone))
            .OrderBy(day => day.Key)
            .Select(day => new DailySales(day.Key, day.Sum(s => s.Quantity), day.Sum(s => s.Revenue)))
            .ToList();

        var report = new EventSalesReport(
            found.Name,
            found.Date,
            found.VenueName,
            found.VenueCity,
            request.DateFrom,
            request.DateTo,
            _timeProvider.GetLocalNow(),
            ticketTypes,
            days);

        return _renderer.RenderEventSales(report);
    }

    private static DateTime StartOfDayUtc(DateOnly day, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTimeToUtc(day.ToDateTime(TimeOnly.MinValue), zone);

    private static DateOnly LocalDate(DateTime paidAtUtc, TimeZoneInfo zone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(paidAtUtc, DateTimeKind.Utc), zone));
}
