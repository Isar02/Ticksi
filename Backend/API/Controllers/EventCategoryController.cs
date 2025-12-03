using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventCategoriesController : ControllerBase
    {
        private readonly IEventCategoryService _service;

        public EventCategoriesController(IEventCategoryService service)
        {
            _service = service;
        }

        // GET all
        [HttpGet]
        public async Task<ActionResult<PagedResult<EventCategoryReadDto>>> GetAll(
            [FromQuery] EventCategoryQueryDto query)
        {
            var result = await _service.GetCategoriesAsync(query);
            return Ok(result);
        }

        // GET by PublicID
        [HttpGet("{publicId:guid}")]
        public async Task<ActionResult<EventCategoryReadDto>> GetEventCategoryByPublicID(Guid publicId)
        {
            var categoryDto = await _service.GetByPublicIdAsync(publicId);
            if (categoryDto == null)
                return NotFound();

            return Ok(categoryDto);
        }

        // POST
        [HttpPost]
        public async Task<ActionResult<EventCategoryReadDto>> Create(EventCategoryCreateDto categoryCreateDto)
        {
            var categoryDto = await _service.CreateAsync(categoryCreateDto);
            return CreatedAtAction(nameof(GetEventCategoryByPublicID), new { publicId = categoryDto.PublicId }, categoryDto);
        }

        // PUT
        [HttpPut("{publicId:guid}")]
        public async Task<ActionResult> Update(Guid publicId, EventCategoryUpdateDto categoryUpdateDto)
        {
            var success = await _service.UpdateAsync(publicId, categoryUpdateDto);
            if (!success)
                return NotFound();

            return NoContent();
        }

        // DELETE
        [HttpDelete("{publicId:guid}")]
        public async Task<ActionResult> Delete(Guid publicId)
        {
            var success = await _service.DeleteAsync(publicId);
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}
