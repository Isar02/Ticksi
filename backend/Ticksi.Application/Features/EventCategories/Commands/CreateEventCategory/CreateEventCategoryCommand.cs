using MediatR;
using Ticksi.Application.DTOs;

namespace Ticksi.Application.Features.EventCategories.Commands.CreateEventCategory
{
    public class CreateEventCategoryCommand : IRequest<EventCategoryReadDto>
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string PosterUrl { get; set; } = string.Empty;
    }
}

