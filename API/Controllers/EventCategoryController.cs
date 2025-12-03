using Ticksi.Application.DTOs;
using Ticksi.Domain.Entities;
using Ticksi.Application.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public async Task<ActionResult<PagedResult<EventCategoryReadDto>>> GetAll(
        [FromQuery] EventCategoryQueryDto query)
        {
            var categories = _repo.Query();

            // SEARCH
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var s = query.Search.Trim().ToLower();
                categories = categories.Where(c =>
                    c.Name.ToLower().Contains(s) ||
                    (c.Description != null && c.Description.ToLower().Contains(s))
                );
            }

            // FILTERING (READY INFRASTRUCTURE)
            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                if (query.Filter == "active")
                    categories = categories.Where(c => c.IsActive);
                if (query.Filter == "inactive")
                    categories = categories.Where(c => !c.IsActive);
            }

            // TOTAL COUNT
            var totalCount = await categories.CountAsync();

            // PAGING
            var items = await categories
                .OrderBy(c => c.Name)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Select(c => new EventCategoryReadDto
                {
                    PublicId = c.PublicId,
                    Name = c.Name,
                    Description = c.Description
                })
                .ToListAsync();

            var result = new PagedResult<EventCategoryReadDto>
            {
                Items = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };

            return Ok(result);
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
                var category = _mapper.Map<EventCategory>(categoryCreateDto);
                await _repo.AddAsync(category);

                var categoryDto = _mapper.Map<EventCategoryReadDto>(category);
                return CreatedAtAction(nameof(GetEventCategoryByPublicID), new { publicId = category.PublicId }, categoryDto);
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
    
