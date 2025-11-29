using API.Data;
using API.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using API.Services;

namespace API.Services
{
    public class EventCategoryRepository : IEventCategoryRepository
    {
        private readonly AppDbContext _context;

        public EventCategoryRepository(AppDbContext context)
        {
            _context = context;
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
