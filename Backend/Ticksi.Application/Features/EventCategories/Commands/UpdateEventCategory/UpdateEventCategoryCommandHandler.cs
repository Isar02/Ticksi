using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.EventCategories.Commands.UpdateEventCategory
{
    public class UpdateEventCategoryCommandHandler : IRequestHandler<UpdateEventCategoryCommand>
    {
        private readonly IAppDbContext _context;

        public UpdateEventCategoryCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task Handle(UpdateEventCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _context.EventCategories
                .FirstOrDefaultAsync(c => c.PublicId == request.PublicId, cancellationToken)
                ?? throw new NotFoundException("Event category not found.");

            category.Name = request.Name;
            category.Description = request.Description;
            category.PosterUrl = request.PosterUrl;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
