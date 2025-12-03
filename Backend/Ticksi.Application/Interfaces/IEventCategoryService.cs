using Ticksi.Application.DTOs;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Interfaces
{
    public interface IEventCategoryService
    {
        Task<PagedResult<EventCategoryReadDto>> GetCategoriesAsync(EventCategoryQueryDto query);
        Task<EventCategoryReadDto?> GetByPublicIdAsync(Guid publicId);
        Task<EventCategoryReadDto> CreateAsync(EventCategoryCreateDto dto);
        Task<bool> UpdateAsync(Guid publicId, EventCategoryUpdateDto dto);
        Task<bool> DeleteAsync(Guid publicId);
    }
}

