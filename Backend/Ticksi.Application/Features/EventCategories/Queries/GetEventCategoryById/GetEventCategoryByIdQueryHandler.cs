using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.EventCategories.Queries.GetEventCategoryById
{
    public class GetEventCategoryByIdQueryHandler : IRequestHandler<GetEventCategoryByIdQuery, EventCategoryReadDto>
    {
        private readonly IAppDbContext _context;

        public GetEventCategoryByIdQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<EventCategoryReadDto> Handle(GetEventCategoryByIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.EventCategories
                .AsNoTracking()
                .Where(c => c.PublicId == request.PublicId)
                .Select(EventCategoryProjections.ToReadDto)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Event category not found.");
        }
    }
}
