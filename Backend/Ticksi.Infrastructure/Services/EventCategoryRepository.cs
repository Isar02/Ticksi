using Ticksi.Infrastructure.Data;
using Ticksi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Interfaces;
using Ticksi.Application.DTOs;

namespace Ticksi.Infrastructure.Services
{
    public class EventCategoryRepository : IEventCategoryRepository
    {
        private readonly AppDbContext _context;

        public EventCategoryRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<EventCategory>> GetPagedCategoriesAsync(EventCategoryQueryDto query)
        {
            var categories = _context.EventCategories.AsQueryable();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchTerm = query.Search.Trim().ToLower();
                categories = categories.Where(c =>
                    c.Name.ToLower().Contains(searchTerm) ||
                    (c.Description != null && c.Description.ToLower().Contains(searchTerm))
                );
            }

            // FILTERING
            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                if (query.Filter.Equals("active", StringComparison.OrdinalIgnoreCase))
                    categories = categories.Where(c => c.IsActive);
                else if (query.Filter.Equals("inactive", StringComparison.OrdinalIgnoreCase))
                    categories = categories.Where(c => !c.IsActive);
            }

            // TOTAL COUNT
            var totalCount = await categories.CountAsync();

            // PAGING
            var items = await categories
                .OrderBy(c => c.Name)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<EventCategory>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<IEnumerable<EventCategory>> GetAllEventCategoriesAsync()
        {
            return await _context.EventCategories.ToListAsync();
        }

        public async Task<EventCategory?> GetByPublicIDAsync(Guid publicId)
        {
            return await _context.EventCategories.FirstOrDefaultAsync(e => e.PublicId == publicId);
        }

        public async Task AddAsync(EventCategory category)
        {
            _context.EventCategories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(EventCategory category)
        {
            _context.EventCategories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(EventCategory category)
        {
            _context.EventCategories.Remove(category);
            await _context.SaveChangesAsync();
        }
    }
}
