using Ticksi.Domain.Entities;

namespace Ticksi.Application.Interfaces
{
    public interface IFavoriteRepository
    {
        Task<Favorite?> GetByUserAndEventAsync(Guid userPublicId, Guid eventPublicId);
        Task AddAsync(Favorite favorite);
        Task DeleteAsync(Favorite favorite);
    }
}

