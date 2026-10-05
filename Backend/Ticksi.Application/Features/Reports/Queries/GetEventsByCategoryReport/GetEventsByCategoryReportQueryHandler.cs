using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Reports.Models;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Reports.Queries.GetEventsByCategoryReport;

public class GetEventsByCategoryReportQueryHandler : IRequestHandler<GetEventsByCategoryReportQuery, byte[]>
{
    private readonly IAppDbContext _context;
    private readonly IReportRenderer _renderer;
    private readonly TimeProvider _timeProvider;

    public GetEventsByCategoryReportQueryHandler(IAppDbContext context, IReportRenderer renderer, TimeProvider timeProvider)
    {
        _context = context;
        _renderer = renderer;
        _timeProvider = timeProvider;
    }

    public async Task<byte[]> Handle(GetEventsByCategoryReportQuery request, CancellationToken cancellationToken)
    {
        var category = await _context.EventCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PublicId == request.CategoryPublicId, cancellationToken)
            ?? throw new NotFoundException("Event category not found.");

        var events = _context.Events
            .AsNoTracking()
            .Where(e => e.EventCategoryId == category.Id);

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

        var rows = await events
            .OrderBy(e => e.Date)
            .Select(e => new EventsByCategoryReportRow(
                e.Name,
                e.Date,
                e.Location!.Name,
                e.Location.City,
                e.Location.Address,
                e.TicketTypes.Min(t => (decimal?)t.Price)))
            .ToListAsync(cancellationToken);

        var report = new EventsByCategoryReport(
            category.Name,
            request.DateFrom,
            request.DateTo,
            _timeProvider.GetLocalNow(),
            rows);

        return _renderer.RenderEventsByCategory(report);
    }
}
