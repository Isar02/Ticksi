using MediatR;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using Ticksi.Application.Features.Reports.Documents;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Reports.Queries.GetEventsByCategoryReport
{
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
            // Find the category by PublicId
            var category = await _context.EventCategories
                .FirstOrDefaultAsync(c => c.PublicId == request.CategoryPublicId, cancellationToken);

            if (category == null)
            {
                throw new KeyNotFoundException($"Event category with ID {request.CategoryPublicId} not found.");
            }

            // Get all events for this category
            var events = await _eventRepository.GetEventsByCategoryAsync(category.Id);

            // Generate PDF using QuestPDF
            var document = new EventsByCategoryReportDocument(category.Name, events);
            var pdfBytes = document.GeneratePdf();

            return pdfBytes;
        }
    }
}

