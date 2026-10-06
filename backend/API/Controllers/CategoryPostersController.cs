using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.Features.Categories.Commands.UploadCategoryPoster;

namespace API.Controllers
{
    [ApiController]
    [Route("api/categories/poster")]
    public class CategoryPostersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public CategoryPostersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Admin,Organizer")] // Only Admin and Organizer can upload category posters
        [ProducesResponseType(typeof(UploadCategoryPosterResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<UploadCategoryPosterResponse>> Upload(
            IFormFile file,
            CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new UploadCategoryPosterCommand { File = file }, cancellationToken);
            return Ok(result);
        }
    }
}
