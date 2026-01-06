using MediatR;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Events.Queries.GetEventImages;
using Ticksi.Application.Features.Events.Queries.GetEvents;
using Ticksi.Domain.Entities;
using Ticksi.Application.Features.Events.Queries.GetEventById;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public EventsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // GET all events with pagination, filtering, and sorting
        [HttpGet]
        public async Task<ActionResult<PagedResult<EventReadDto>>> GetAll(
            [FromQuery] GetEventsQuery query,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{eventId:guid}/images")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<string>>> GetEventImages(
            Guid eventId,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(
                new GetEventImagesQuery(eventId),
                cancellationToken
            );

            if (result == null)
                return NotFound("Event not found.");

            return Ok(result);
        }

        // GET event by ID
        [HttpGet("{eventId:guid}")]
        [ProducesResponseType(typeof(EventReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventReadDto>> GetById(Guid eventId, CancellationToken cancellationToken)
        {
            var dto = await _mediator.Send(new GetEventByIdQuery(eventId), cancellationToken);
            if (dto == null) return NotFound("Event not found.");
            return Ok(dto);
        }
    }
}


