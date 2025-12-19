using Ticksi.Domain.Entities;

namespace Ticksi.Application.Interfaces
{
    public interface IFavoriteRepository
    {
        Task<Favorite?> GetByUserAndEventAsync(Guid userPublicId, Guid eventPublicId);
        Task<List<Guid>> GetUserFavoritesAsync(Guid userPublicId);
        Task AddAsync(Favorite favorite);
        Task DeleteAsync(Favorite favorite);
    }
}

