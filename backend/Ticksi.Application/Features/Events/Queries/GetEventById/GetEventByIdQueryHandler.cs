using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Events.Queries.GetEventById
{
    public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventReadDto>
    {
        private readonly IAppDbContext _context;

        public GetEventByIdQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<EventReadDto> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
        {
            return await _context.Events
                .AsNoTracking()
                .Where(e => e.PublicId == request.EventId)
                .Select(EventProjections.ToReadDto)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new NotFoundException("Event not found.");
        }
    }
}
