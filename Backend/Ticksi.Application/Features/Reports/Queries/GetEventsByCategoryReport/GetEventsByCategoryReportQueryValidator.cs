using FluentValidation;

namespace Ticksi.Application.Features.Reports.Queries.GetEventsByCategoryReport;

public class GetEventsByCategoryReportQueryValidator : AbstractValidator<GetEventsByCategoryReportQuery>
{
    public GetEventsByCategoryReportQueryValidator()
    {
        RuleFor(x => x.CategoryPublicId).NotEmpty().WithMessage("Category is required.");
    }
}
