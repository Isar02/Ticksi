using API.Entities;
using System.Threading.Tasks;

namespace API.Services
{
    public interface IEventCategoryRepository
    {

        Task<IEnumerable<EventCategory>> GetAllEventCategoriesAsync();
        Task<EventCategory?> GetByPublicIDAsync(Guid publicId);
        Task AddAsync(EventCategory eventCategory);
        Task UpdateAsync(EventCategory eventCategory);
        Task DeleteAsync(EventCategory eventCategory);
    }
}
