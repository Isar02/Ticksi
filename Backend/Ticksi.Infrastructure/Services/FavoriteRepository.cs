using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;
using Ticksi.Infrastructure.Data;

namespace Ticksi.Infrastructure.Services
{
    public class FavoriteRepository : IFavoriteRepository
    {
        private readonly AppDbContext _context;

        public FavoriteRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Favorite?> GetByUserAndEventAsync(Guid userPublicId, Guid eventPublicId)
        {
            return await _context.Favorites
                .FirstOrDefaultAsync(f => 
                    f.AppUser!.PublicId == userPublicId && 
                    f.Event!.PublicId == eventPublicId);
        }

        public async Task AddAsync(Favorite favorite)
        {
            await _context.Favorites.AddAsync(favorite);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Favorite favorite)
        {
            _context.Favorites.Remove(favorite);
            await _context.SaveChangesAsync();
        }
    }
}

