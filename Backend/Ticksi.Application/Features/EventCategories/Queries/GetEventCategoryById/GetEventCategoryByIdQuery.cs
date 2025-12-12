using MediatR;
using Ticksi.Application.DTOs;

namespace Ticksi.Application.Features.EventCategories.Queries.GetEventCategoryById
{
    public class GetEventCategoryByIdQuery : IRequest<EventCategoryReadDto?>
    {
        public Guid PublicId { get; set; }
    }
}

