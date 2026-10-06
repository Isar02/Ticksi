using API.Authorization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.Features.Reports.Queries.GetEventSalesReport;
using Ticksi.Application.Features.Reports.Queries.GetEventsByCategoryReport;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = ApiRoles.EventManagers)]
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
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var query = new GetEventsByCategoryReportQuery
        {
            CategoryPublicId = categoryPublicId,
            DateFrom = dateFrom,
            DateTo = dateTo
        };
        var pdfBytes = await _mediator.Send(query, cancellationToken);

        return File(pdfBytes, "application/pdf", $"events-report-{categoryPublicId}.pdf");
    }

    [HttpGet("event-sales/{eventPublicId:guid}")]
    public async Task<IActionResult> GetEventSalesReport(
        Guid eventPublicId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        var query = new GetEventSalesReportQuery
        {
            EventPublicId = eventPublicId,
            DateFrom = dateFrom,
            DateTo = dateTo
        };
        var pdfBytes = await _mediator.Send(query, cancellationToken);

        return File(pdfBytes, "application/pdf", $"sales-report-{eventPublicId}.pdf");
    }
}
