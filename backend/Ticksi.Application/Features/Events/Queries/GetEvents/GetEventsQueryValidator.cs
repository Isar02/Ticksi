using FluentValidation;
using Ticksi.Application.Common;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Events.Queries.GetEvents;

public class GetEventsQueryValidator : AbstractValidator<GetEventsQuery>
{
    private static readonly string[] SortFields = ["name", "date", "price"];

    public GetEventsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PagingExtensions.MaxPageSize)
            .WithMessage($"Page size must be between 1 and {PagingExtensions.MaxPageSize}.");

        RuleFor(x => x.Search)
            .MaximumLength(100).WithMessage("Search cannot exceed 100 characters.");

        RuleFor(x => x.City)
            .Must(c => c!.Trim().Length <= Location.Constraints.CityMaxLength)
            .When(x => x.City is not null)
            .WithMessage($"City cannot exceed {Location.Constraints.CityMaxLength} characters.");

        RuleFor(x => x.SortBy)
            .Must(s => SortFields.Contains(s!, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage("Sort must be one of: name, date, price.");

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum price cannot be negative.");

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithMessage("Maximum price cannot be lower than the minimum price.");

        RuleFor(x => x.DateTo)
            .GreaterThanOrEqualTo(x => x.DateFrom)
            .When(x => x.DateFrom.HasValue && x.DateTo.HasValue)
            .WithMessage("End date cannot be before the start date.");
    }
}
