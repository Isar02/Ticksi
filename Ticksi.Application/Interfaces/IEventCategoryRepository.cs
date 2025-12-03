using Ticksi.Domain.Entities;
using System.Threading.Tasks;

namespace Ticksi.Application.Interfaces
{
    public interface IEventCategoryRepository
    {

        IQueryable<EventCategory> Query();
        Task<IEnumerable<EventCategory>> GetAllEventCategoriesAsync();
        Task<EventCategory?> GetByPublicIDAsync(Guid publicId);
        Task AddAsync(EventCategory eventCategory);
        Task UpdateAsync(EventCategory eventCategory);
        Task DeleteAsync(EventCategory eventCategory);
    }
}
