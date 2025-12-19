using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.Features.Favorites.Commands.AddFavorite;
using Ticksi.Application.Features.Favorites.Commands.RemoveFavorite;
using Ticksi.Application.Features.Favorites.Queries.GetUserFavorites;

namespace API.Controllers
{
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

        // GET: api/favorites
        [HttpGet]
        public async Task<ActionResult<List<Guid>>> GetUserFavorites(CancellationToken cancellationToken)
        {
            // Extract user's PublicId from JWT token
            var userPublicIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userPublicIdClaim) || !Guid.TryParse(userPublicIdClaim, out var userPublicId))
            {
                return Unauthorized("Invalid user token.");
            }

            var query = new GetUserFavoritesQuery { UserPublicId = userPublicId };
            var favoriteIds = await _mediator.Send(query, cancellationToken);

            return Ok(favoriteIds);
        }

        // POST: api/favorites/{eventPublicId}
        [HttpPost("{eventPublicId:guid}")]
        public async Task<ActionResult> AddFavorite(
            Guid eventPublicId,
            CancellationToken cancellationToken)
        {
            // Extract user's PublicId from JWT token
            var userPublicIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userPublicIdClaim) || !Guid.TryParse(userPublicIdClaim, out var userPublicId))
            {
                return Unauthorized("Invalid user token.");
            }

            var command = new AddFavoriteCommand
            {
                UserPublicId = userPublicId,
                EventPublicId = eventPublicId
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
            {
                return BadRequest("Unable to add favorite. It may already exist or the event/user was not found.");
            }

            return Ok(new { message = "Event added to favorites successfully." });
        }

        // DELETE: api/favorites/{eventPublicId}
        [HttpDelete("{eventPublicId:guid}")]
        public async Task<ActionResult> RemoveFavorite(
            Guid eventPublicId,
            CancellationToken cancellationToken)
        {
            // Extract user's PublicId from JWT token
            var userPublicIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userPublicIdClaim) || !Guid.TryParse(userPublicIdClaim, out var userPublicId))
            {
                return Unauthorized("Invalid user token.");
            }

            var command = new RemoveFavoriteCommand
            {
                UserPublicId = userPublicId,
                EventPublicId = eventPublicId
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (!result)
            {
                return NotFound("Favorite not found.");
            }

            return Ok(new { message = "Event removed from favorites successfully." });
        }
    }
}

