using MediatR;
using Ticksi.Application.Common;
using Ticksi.Application.DTOs;

namespace Ticksi.Application.Features.EventCategories.Queries.GetEventCategories
{
    public class GetEventCategoriesQuery : IRequest<PagedResult<EventCategoryReadDto>>
    {
        public string? Search { get; set; }
        public string? Filter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 8;
    }
}
