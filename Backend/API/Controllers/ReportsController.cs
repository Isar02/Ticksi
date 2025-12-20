using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.Features.Reports.Queries.GetEventsByCategoryReport;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Organizer")] // Only Admin and Organizer can download reports
    public class ReportsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ReportsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Generate PDF report of all events for a specific category
        /// </summary>
        /// <param name="categoryPublicId">The PublicId of the event category</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>PDF file download</returns>
        [HttpGet("events-by-category/{categoryPublicId:guid}")]
        public async Task<IActionResult> GetEventsByCategoryReport(
            Guid categoryPublicId,
            CancellationToken cancellationToken)
        {
            try
            {
                var query = new GetEventsByCategoryReportQuery
                {
                    CategoryPublicId = categoryPublicId
                };

                var pdfBytes = await _mediator.Send(query, cancellationToken);

                // Return PDF file with appropriate content type and filename
                return File(
                    pdfBytes,
                    "application/pdf",
                    $"events-report-{categoryPublicId}.pdf"
                );
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while generating the report.", details = ex.Message });
            }
        }
    }
}

