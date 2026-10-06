using MediatR;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.EventCategories.Commands.CreateEventCategory
{
    public class CreateEventCategoryCommandHandler : IRequestHandler<CreateEventCategoryCommand, EventCategoryReadDto>
    {
        private readonly IAppDbContext _context;

        public CreateEventCategoryCommandHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<EventCategoryReadDto> Handle(CreateEventCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = new EventCategory
            {
                Name = request.Name,
                Description = request.Description,
                PosterUrl = request.PosterUrl
            };

            _context.EventCategories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return new EventCategoryReadDto
            {
                PublicId = category.PublicId,
                Name = category.Name,
                Description = category.Description,
                PosterUrl = category.PosterUrl
            };
        }
    }
}
