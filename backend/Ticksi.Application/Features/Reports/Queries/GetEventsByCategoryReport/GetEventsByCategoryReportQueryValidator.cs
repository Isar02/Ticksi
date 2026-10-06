using FluentValidation;

namespace Ticksi.Application.Features.Reports.Queries.GetEventsByCategoryReport;

public class GetEventsByCategoryReportQueryValidator : AbstractValidator<GetEventsByCategoryReportQuery>
{
    public GetEventsByCategoryReportQueryValidator()
    {
        RuleFor(x => x.CategoryPublicId).NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("End date cannot be before the start date.");
    }
}
