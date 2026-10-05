using FluentValidation;
using Ticksi.Application.Common;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    private static readonly string[] SortFields = ["name", "email", "role", "registered"];

    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page must be 1 or greater.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, PagingExtensions.MaxPageSize)
            .WithMessage($"Page size must be between 1 and {PagingExtensions.MaxPageSize}.");

        RuleFor(x => x.Search)
            .MaximumLength(AppUser.Constraints.EmailMaxLength)
            .WithMessage("Search cannot exceed {MaxLength} characters.");

        RuleFor(x => x.SortBy)
            .Must(s => SortFields.Contains(s!, StringComparer.OrdinalIgnoreCase))
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage($"Sort must be one of: {string.Join(", ", SortFields)}.");

        RuleFor(x => x.RegisteredTo)
            .GreaterThanOrEqualTo(x => x.RegisteredFrom)
            .When(x => x.RegisteredFrom.HasValue && x.RegisteredTo.HasValue)
            .WithMessage("End date cannot be before the start date.");
    }
}
