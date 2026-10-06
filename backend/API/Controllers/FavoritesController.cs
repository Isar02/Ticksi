using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Favorites.Commands.AddFavorite;
using Ticksi.Application.Features.Favorites.Commands.RemoveFavorite;
using Ticksi.Application.Features.Favorites.Queries.GetFavoriteEvents;
using Ticksi.Application.Features.Favorites.Queries.GetUserFavorites;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FavoritesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<Guid>>> GetUserFavorites(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetUserFavoritesQuery(), cancellationToken));
    }

    [HttpGet("events")]
    public async Task<ActionResult<List<EventReadDto>>> GetFavoriteEvents(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetFavoriteEventsQuery(), cancellationToken));
    }

    [HttpPost("{eventPublicId:guid}")]
    public async Task<IActionResult> AddFavorite(Guid eventPublicId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new AddFavoriteCommand { EventPublicId = eventPublicId }, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{eventPublicId:guid}")]
    public async Task<IActionResult> RemoveFavorite(Guid eventPublicId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveFavoriteCommand { EventPublicId = eventPublicId }, cancellationToken);
        return NoContent();
    }
}
