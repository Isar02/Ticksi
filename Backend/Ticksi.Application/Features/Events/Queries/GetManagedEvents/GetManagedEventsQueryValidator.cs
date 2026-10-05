using FluentValidation;
using Ticksi.Application.Common;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Events.Queries.GetManagedEvents;

public class GetManagedEventsQueryValidator : AbstractValidator<GetManagedEventsQuery>
{
    private static readonly string[] SortFields = ["name", "date", "venue", "sold"];
    private static readonly string[] Periods = ["upcoming", "past"];

    public GetManagedEventsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PagingExtensions.MaxPageSize)
            .WithMessage($"Page size must be between 1 and {PagingExtensions.MaxPageSize}.");

        RuleFor(x => x.Name)
            .MaximumLength(Event.Constraints.NameMaxLength)
            .WithMessage("Name cannot exceed {MaxLength} characters.");

        RuleFor(x => x.SortBy)
            .Must(s => SortFields.Contains(s!, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage($"Sort must be one of: {string.Join(", ", SortFields)}.");

        RuleFor(x => x.Period)
            .Must(p => Periods.Contains(p!, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.Period))
            .WithMessage($"Period must be one of: {string.Join(", ", Periods)}.");

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("End date cannot be before the start date.");
    }
}
