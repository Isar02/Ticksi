using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.EventCategories.Commands.CreateEventCategory;
using Ticksi.Application.Features.EventCategories.Commands.DeleteEventCategory;
using Ticksi.Application.Features.EventCategories.Commands.UpdateEventCategory;
using Ticksi.Application.Features.EventCategories.Queries.GetEventCategories;
using Ticksi.Application.Features.EventCategories.Queries.GetEventCategoryById;
using Ticksi.Domain.Entities;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventCategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EventCategoriesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET all
        [HttpGet]
        public async Task<ActionResult<PagedResult<EventCategoryReadDto>>> GetAll(
            [FromQuery] GetEventCategoriesQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        // GET by PublicID
        [HttpGet("{publicId:guid}")]
        public async Task<ActionResult<EventCategoryReadDto>> GetEventCategoryByPublicID(
            Guid publicId,
            CancellationToken cancellationToken)
        {
            var query = new GetEventCategoryByIdQuery { PublicId = publicId };
            var categoryDto = await _mediator.Send(query, cancellationToken);
            
            if (categoryDto == null)
                return NotFound();

            return Ok(categoryDto);
        }

        // POST
        [HttpPost]
        [Authorize(Roles = "Admin,Organizer")] // Only Admin and Organizer can create categories
        public async Task<ActionResult<EventCategoryReadDto>> Create(
            CreateEventCategoryCommand command,
            CancellationToken cancellationToken)
        {
            var categoryDto = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetEventCategoryByPublicID), new { publicId = categoryDto.PublicId }, categoryDto);
        }

        // PUT
        [HttpPut("{publicId:guid}")]
        [Authorize(Roles = "Admin,Organizer")] // Only Admin and Organizer can update categories
        public async Task<ActionResult> Update(
            Guid publicId,
            [FromBody] UpdateEventCategoryCommand command,
            CancellationToken cancellationToken)
        {
            command.PublicId = publicId;
            var success = await _mediator.Send(command, cancellationToken);
            
            if (!success)
                return NotFound();

            return NoContent();
        }

        // DELETE
        [HttpDelete("{publicId:guid}")]
        [Authorize(Roles = "Admin,Organizer")] // Only Admin and Organizer can delete categories
        public async Task<ActionResult> Delete(
            Guid publicId,
            CancellationToken cancellationToken)
        {
            var command = new DeleteEventCategoryCommand { PublicId = publicId };
            var success = await _mediator.Send(command, cancellationToken);
            
            if (!success)
                return NotFound();

            return NoContent();
        }
    }
}
