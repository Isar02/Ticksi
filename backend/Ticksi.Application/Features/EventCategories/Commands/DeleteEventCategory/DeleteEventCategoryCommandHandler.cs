using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.EventCategories.Commands.DeleteEventCategory
{
    public class DeleteEventCategoryCommandHandler : IRequestHandler<DeleteEventCategoryCommand>
    {
        private readonly IAppDbContext _context;

        public DeleteEventCategoryCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task Handle(DeleteEventCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _context.EventCategories
                .FirstOrDefaultAsync(c => c.PublicId == request.PublicId, cancellationToken)
                ?? throw new NotFoundException("Event category not found.");

            if (await _context.Events.AnyAsync(e => e.EventCategoryId == category.Id, cancellationToken))
                throw new ConflictException("The category still has events and cannot be deleted.");

            _context.EventCategories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
