using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.Common;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Events.Commands.CreateEvent;
using Ticksi.Application.Features.Events.Commands.DeleteEvent;
using Ticksi.Application.Features.Events.Commands.UpdateEvent;
using Ticksi.Application.Features.Events.Commands.UploadEventPoster;
using Ticksi.Application.Features.Events.Queries.GetCatalogueFilters;
using Ticksi.Application.Features.Events.Queries.GetEventById;
using Ticksi.Application.Features.Events.Queries.GetEventForEdit;
using Ticksi.Application.Features.Events.Queries.GetEventFormOptions;
using Ticksi.Application.Features.Events.Queries.GetEventImages;
using Ticksi.Application.Features.Events.Queries.GetEventTicketTypes;
using Ticksi.Application.Features.Events.Queries.GetEvents;
using Ticksi.Application.Features.Events.Queries.GetManagedEvents;
using Ticksi.Domain.Entities;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private const string EventManagers = $"{Role.Names.Admin},{Role.Names.Organizer}";

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

        [HttpGet("catalogue-filters")]
        public async Task<ActionResult<CatalogueFiltersDto>> GetCatalogueFilters(CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new GetCatalogueFiltersQuery(), cancellationToken));
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

            return Ok(result);
        }

        [HttpGet("{eventId:guid}/ticket-types")]
        [ProducesResponseType(typeof(List<EventTicketTypeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<EventTicketTypeDto>>> GetTicketTypes(
            Guid eventId,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new GetEventTicketTypesQuery(eventId), cancellationToken));
        }

        // GET event by ID
        [HttpGet("{eventId:guid}")]
        [ProducesResponseType(typeof(EventReadDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventReadDto>> GetById(Guid eventId, CancellationToken cancellationToken)
        {
            var dto = await _mediator.Send(new GetEventByIdQuery(eventId), cancellationToken);
            return Ok(dto);
        }

        [HttpGet("managed")]
        [Authorize(Roles = EventManagers)]
        public async Task<ActionResult<PagedResult<ManagedEventDto>>> GetManaged(
            [FromQuery] GetManagedEventsQuery query,
            CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(query, cancellationToken));
        }

        [HttpGet("{eventId:guid}/edit")]
        [Authorize(Roles = EventManagers)]
        [ProducesResponseType(typeof(EventForEditDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<EventForEditDto>> GetForEdit(Guid eventId, CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new GetEventForEditQuery(eventId), cancellationToken));
        }

        [HttpGet("form-options")]
        [Authorize(Roles = EventManagers)]
        public async Task<ActionResult<EventFormOptionsDto>> GetFormOptions(CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new GetEventFormOptionsQuery(), cancellationToken));
        }

        [HttpPost]
        [Authorize(Roles = EventManagers)]
        [ProducesResponseType(typeof(EventReadDto), StatusCodes.Status201Created)]
        public async Task<ActionResult<EventReadDto>> Create(
            CreateEventCommand command,
            CancellationToken cancellationToken)
        {
            var dto = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { eventId = dto.PublicId }, dto);
        }

        [HttpPut("{eventId:guid}")]
        [Authorize(Roles = EventManagers)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> Update(
            Guid eventId,
            UpdateEventCommand command,
            CancellationToken cancellationToken)
        {
            command.PublicId = eventId;
            await _mediator.Send(command, cancellationToken);
            return NoContent();
        }

        [HttpPut("{eventId:guid}/poster")]
        [Authorize(Roles = EventManagers)]
        [ProducesResponseType(typeof(EventPosterDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<EventPosterDto>> UploadPoster(
            Guid eventId,
            IFormFile? file,
            CancellationToken cancellationToken)
        {
            var command = new UploadEventPosterCommand { PublicId = eventId, File = file };
            return Ok(await _mediator.Send(command, cancellationToken));
        }

        [HttpDelete("{eventId:guid}")]
        [Authorize(Roles = EventManagers)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult> Delete(Guid eventId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new DeleteEventCommand { PublicId = eventId }, cancellationToken);
            return NoContent();
        }
    }
}


