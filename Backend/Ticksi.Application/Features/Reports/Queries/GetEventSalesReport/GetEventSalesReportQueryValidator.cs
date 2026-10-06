using FluentValidation;

namespace Ticksi.Application.Features.Reports.Queries.GetEventSalesReport;

public class GetEventSalesReportQueryValidator : AbstractValidator<GetEventSalesReportQuery>
{
    public GetEventSalesReportQueryValidator()
    {
        RuleFor(x => x.EventPublicId).NotEmpty().WithMessage("Event is required.");

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("End date cannot be before the start date.");
    }
}
