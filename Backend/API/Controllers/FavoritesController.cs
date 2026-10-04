using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Favorites.Commands.AddFavorite;
using Ticksi.Application.Features.Favorites.Commands.RemoveFavorite;
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
        var query = new GetUserFavoritesQuery { UserPublicId = CurrentUserPublicId() };
        return Ok(await _mediator.Send(query, cancellationToken));
    }

    [HttpPost("{eventPublicId:guid}")]
    public async Task<IActionResult> AddFavorite(Guid eventPublicId, CancellationToken cancellationToken)
    {
        var command = new AddFavoriteCommand { UserPublicId = CurrentUserPublicId(), EventPublicId = eventPublicId };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    [HttpDelete("{eventPublicId:guid}")]
    public async Task<IActionResult> RemoveFavorite(Guid eventPublicId, CancellationToken cancellationToken)
    {
        var command = new RemoveFavoriteCommand { UserPublicId = CurrentUserPublicId(), EventPublicId = eventPublicId };
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    private Guid CurrentUserPublicId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userPublicId)
            ? userPublicId
            : throw new UnauthorizedException("Your session is not valid. Please sign in again.");
}
