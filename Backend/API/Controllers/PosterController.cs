using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.Features.Posters.Commands.UploadPoster;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public PostersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Uploads a poster image for an event.
        /// </summary>
        /// <param name="file">The image file to upload (jpg, jpeg, png, gif, webp)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The URL and metadata of the uploaded file</returns>
        /// <response code="200">Returns the uploaded file information</response>
        /// <response code="400">If the file is invalid (wrong type, too large, or empty)</response>
        /// <response code="401">If the user is not authenticated</response>
        [HttpPost("upload")]
        [Authorize]
        [ProducesResponseType(typeof(UploadPosterResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<UploadPosterResponse>> Upload(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var command = new UploadPosterCommand { File = file };
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
    }
}

