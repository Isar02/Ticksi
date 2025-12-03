using AutoMapper;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Services
{
    public class EventCategoryService : IEventCategoryService
    {
        private readonly IEventCategoryRepository _repository;
        private readonly IMapper _mapper;

        public EventCategoryService(IEventCategoryRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PagedResult<EventCategoryReadDto>> GetCategoriesAsync(EventCategoryQueryDto query)
        {
            // Get filtered and paginated results from repository
            var pagedResult = await _repository.GetPagedCategoriesAsync(query);

            // Map entities to DTOs
            var dtos = _mapper.Map<List<EventCategoryReadDto>>(pagedResult.Items);

            return new PagedResult<EventCategoryReadDto>
            {
                Items = dtos,
                Page = pagedResult.Page,
                PageSize = pagedResult.PageSize,
                TotalCount = pagedResult.TotalCount
            };
        }

        public async Task<EventCategoryReadDto?> GetByPublicIdAsync(Guid publicId)
        {
            var category = await _repository.GetByPublicIDAsync(publicId);
            if (category == null)
                return null;

            return _mapper.Map<EventCategoryReadDto>(category);
        }

        public async Task<EventCategoryReadDto> CreateAsync(EventCategoryCreateDto dto)
        {
            var category = _mapper.Map<EventCategory>(dto);
            await _repository.AddAsync(category);

            return _mapper.Map<EventCategoryReadDto>(category);
        }

        public async Task<bool> UpdateAsync(Guid publicId, EventCategoryUpdateDto dto)
        {
            var existingCategory = await _repository.GetByPublicIDAsync(publicId);
            if (existingCategory == null)
                return false;

            _mapper.Map(dto, existingCategory);
            await _repository.UpdateAsync(existingCategory);

            return true;
        }

        public async Task<bool> DeleteAsync(Guid publicId)
        {
            var existingCategory = await _repository.GetByPublicIDAsync(publicId);
            if (existingCategory == null)
                return false;

            await _repository.DeleteAsync(existingCategory);
            return true;
        }
    }
}

