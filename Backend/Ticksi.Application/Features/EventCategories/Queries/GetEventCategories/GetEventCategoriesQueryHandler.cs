using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.EventCategories.Queries.GetEventCategories
{
    public class GetEventCategoriesQueryHandler : IRequestHandler<GetEventCategoriesQuery, PagedResult<EventCategoryReadDto>>
    {
        private readonly IAppDbContext _context;

        public GetEventCategoriesQueryHandler(IAppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<EventCategoryReadDto>> Handle(GetEventCategoriesQuery request, CancellationToken cancellationToken)
        {
            var categories = _context.EventCategories.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim();
                categories = categories.Where(c => c.Name.Contains(term) || c.Description.Contains(term));
            }

            if (string.Equals(request.Filter, "active", StringComparison.OrdinalIgnoreCase))
                categories = categories.Where(c => c.IsActive);
            else if (string.Equals(request.Filter, "inactive", StringComparison.OrdinalIgnoreCase))
                categories = categories.Where(c => !c.IsActive);

            return await categories
                .OrderBy(c => c.Name)
                .ThenBy(c => c.Id)
                .Select(EventCategoryProjections.ToReadDto)
                .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
        }
    }
}
