using API.DTOs;
using API.Services;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
   
        [ApiController]
        [Route("api/[controller]")]
        public class EventCategoriesController : ControllerBase
        {
            private readonly IMapper _mapper;
            private readonly IEventCategoryRepository _repo;
            public EventCategoriesController(IEventCategoryRepository repo, IMapper mapper)
            {
                _mapper = mapper;
                _repo = repo;
            }

            //GET all
            [HttpGet]
            public async Task<ActionResult<IEnumerable<EventCategoryReadDto>>> GetEventCategories()
            {
                var categories = await _repo.GetAllEventCategoriesAsync();
                var categoriesDto = _mapper.Map<IEnumerable<EventCategoryReadDto>>(categories);
                return Ok(categoriesDto);
            }

            //GET by PublicID
            [HttpGet("{publicId:guid}")]
            public async Task<ActionResult<EventCategoryReadDto>> GetEventCategoryByPublicID(Guid publicId)
            {
                var existingCategory = await _repo.GetByPublicIDAsync(publicId);
                if (existingCategory == null) return NotFound();

                var categoryDto = _mapper.Map<EventCategoryReadDto>(existingCategory);
                return Ok(categoryDto);
            }

            //Post
            [HttpPost]
            public async Task<ActionResult<EventCategoryReadDto>> Create(EventCategoryCreateDto categoryCreateDto)
            {
                var category = _mapper.Map<API.Entities.EventCategory>(categoryCreateDto);
                await _repo.AddAsync(category);

                var categoryDto = _mapper.Map<EventCategoryReadDto>(category);
                return CreatedAtAction(nameof(GetEventCategories), new { publicId = category.PublicId }, categoryDto);
            }

            //Put
            [HttpPut("{publicId:guid}")]
            public async Task<ActionResult> Update(Guid publicId, EventCategoryCreateDto categoryUpdateDto)
            {
                var existingCategory = await _repo.GetByPublicIDAsync(publicId);
                if (existingCategory == null) return NotFound();
                _mapper.Map(categoryUpdateDto, existingCategory);
                await _repo.UpdateAsync(existingCategory);
                return NoContent();
            }

            //Delete
            [HttpDelete("{publicId}")]
            public async Task<ActionResult> Delete(Guid publicId)
            {
                var existingCategory = await _repo.GetByPublicIDAsync(publicId);
                if (existingCategory == null) return NotFound();
                await _repo.DeleteAsync(existingCategory);
                return NoContent();
            }

        }
      }
    
