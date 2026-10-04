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
    private readonly IEventRepository _eventRepository;

    public GetEventsByCategoryReportQueryHandler(IAppDbContext context, IEventRepository eventRepository)
    {
        _context = context;
        _eventRepository = eventRepository;
    }

    public async Task<byte[]> Handle(GetEventsByCategoryReportQuery request, CancellationToken cancellationToken)
    {
        var category = await _context.EventCategories
            .FirstOrDefaultAsync(c => c.PublicId == request.CategoryPublicId, cancellationToken)
            ?? throw new NotFoundException("Event category not found.");

        var events = await _eventRepository.GetEventsByCategoryAsync(category.Id);

        return new EventsByCategoryReportDocument(category.Name, events).GeneratePdf();
    }
}
