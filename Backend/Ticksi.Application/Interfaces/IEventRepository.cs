using Ticksi.Application.DTOs;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Interfaces
{
    public interface IEventRepository
    {
        Task<PagedResult<Event>> GetPagedEventsAsync(EventQueryDto query);
        Task<Event?> GetByPublicIdAsync(Guid publicId);
        Task<List<Event>> GetEventsByCategoryAsync(int categoryId);
        Task AddAsync(Event eventEntity);
        Task UpdateAsync(Event eventEntity);
        Task DeleteAsync(Event eventEntity);
    }
}


