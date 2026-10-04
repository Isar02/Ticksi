using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Reports.Documents;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Reports.Queries.GetEventsByCategoryReport;

public class GetEventsByCategoryReportQueryHandler : IRequestHandler<GetEventsByCategoryReportQuery, byte[]>
{
    private readonly IAppDbContext _context;

    public GetEventsByCategoryReportQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> Handle(GetEventsByCategoryReportQuery request, CancellationToken cancellationToken)
    {
        var category = await _context.EventCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PublicId == request.CategoryPublicId, cancellationToken)
            ?? throw new NotFoundException("Event category not found.");

        var events = await _context.Events
            .AsNoTracking()
            .Where(e => e.EventCategoryId == category.Id)
            .OrderBy(e => e.Date)
            .Select(e => new EventsByCategoryReportRow(
                e.Name,
                e.Date,
                e.Location!.Name,
                e.Location.City,
                e.Location.Address,
                e.TicketTypes.Min(t => (decimal?)t.Price)))
            .ToListAsync(cancellationToken);

        return new EventsByCategoryReportDocument(category.Name, events).GeneratePdf();
    }
}
