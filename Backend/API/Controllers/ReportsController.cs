using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.Features.Reports.Queries.GetEventsByCategoryReport;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Organizer")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("events-by-category/{categoryPublicId:guid}")]
    public async Task<IActionResult> GetEventsByCategoryReport(
        Guid categoryPublicId,
        CancellationToken cancellationToken)
    {
        var query = new GetEventsByCategoryReportQuery { CategoryPublicId = categoryPublicId };
        var pdfBytes = await _mediator.Send(query, cancellationToken);

        return File(pdfBytes, "application/pdf", $"events-report-{categoryPublicId}.pdf");
    }
}
