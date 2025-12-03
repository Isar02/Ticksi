using Ticksi.Application.DTOs;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Interfaces
{
    public interface IEventCategoryRepository
    {
        Task<PagedResult<EventCategory>> GetPagedCategoriesAsync(EventCategoryQueryDto query);
        Task<IEnumerable<EventCategory>> GetAllEventCategoriesAsync();
        Task<EventCategory?> GetByPublicIDAsync(Guid publicId);
        Task AddAsync(EventCategory eventCategory);
        Task UpdateAsync(EventCategory eventCategory);
        Task DeleteAsync(EventCategory eventCategory);
    }
}
