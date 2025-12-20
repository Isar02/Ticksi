using Ticksi.Infrastructure.Data;
using Ticksi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Interfaces;
using Ticksi.Application.DTOs;

namespace Ticksi.Infrastructure.Services
{
    public class EventRepository : IEventRepository
    {
        private readonly AppDbContext _context;

        public EventRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<Event>> GetPagedEventsAsync(EventQueryDto query)
        {
            var events = _context.Events
                .Include(e => e.EventCategory)
                .Include(e => e.Location)
                .Include(e => e.EventType)
                .Include(e => e.OrganizerCompany)
                .AsQueryable();

            // SEARCH - Search in Name and Description
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var searchTerm = query.Search.Trim().ToLower();
                events = events.Where(e =>
                    e.Name.ToLower().Contains(searchTerm) ||
                    (e.Description != null && e.Description.ToLower().Contains(searchTerm))
                );
            }

            // FILTER - By Category
            if (query.CategoryId.HasValue)
            {
                events = events.Where(e => e.EventCategory != null && e.EventCategory.PublicId == query.CategoryId.Value);
            }

            // FILTER - By Date Range
            if (query.DateFrom.HasValue)
            {
                events = events.Where(e => e.Date >= query.DateFrom.Value);
            }

            if (query.DateTo.HasValue)
            {
                events = events.Where(e => e.Date <= query.DateTo.Value);
            }

            // FILTER - By Price Range
            if (query.MinPrice.HasValue)
            {
                events = events.Where(e => e.Price >= query.MinPrice.Value);
            }

            if (query.MaxPrice.HasValue)
            {
                events = events.Where(e => e.Price <= query.MaxPrice.Value);
            }

            // SORTING - Apply sorting based on SortBy parameter
            if (!string.IsNullOrWhiteSpace(query.SortBy))
            {
                events = query.SortBy.ToLower() switch
                {
                    "name" => query.SortDescending 
                        ? events.OrderByDescending(e => e.Name) 
                        : events.OrderBy(e => e.Name),
                    "date" => query.SortDescending 
                        ? events.OrderByDescending(e => e.Date) 
                        : events.OrderBy(e => e.Date),
                    "price" => query.SortDescending 
                        ? events.OrderByDescending(e => e.Price) 
                        : events.OrderBy(e => e.Price),
                    _ => events.OrderBy(e => e.Date) // Default sort by Date
                };
            }
            else
            {
                // Default sorting by Date ascending
                events = events.OrderBy(e => e.Date);
            }

            // TOTAL COUNT - Get count before pagination
            var totalCount = await events.CountAsync();

            // PAGINATION - Skip and Take
            var items = await events
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return new PagedResult<Event>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };
        }

        public async Task<Event?> GetByPublicIdAsync(Guid publicId)
        {
            return await _context.Events
                .Include(e => e.EventCategory)
                .Include(e => e.Location)
                .Include(e => e.EventType)
                .Include(e => e.OrganizerCompany)
                .FirstOrDefaultAsync(e => e.PublicId == publicId);
        }

        public async Task<List<Event>> GetEventsByCategoryAsync(int categoryId)
        {
            return await _context.Events
                .Include(e => e.EventCategory)
                .Include(e => e.Location)
                .Include(e => e.EventType)
                .Include(e => e.OrganizerCompany)
                .Where(e => e.EventCategoryId == categoryId)
                .OrderBy(e => e.Date)
                .ToListAsync();
        }

        public async Task AddAsync(Event eventEntity)
        {
            _context.Events.Add(eventEntity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Event eventEntity)
        {
            _context.Events.Update(eventEntity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Event eventEntity)
        {
            _context.Events.Remove(eventEntity);
            await _context.SaveChangesAsync();
        }
    }
}


