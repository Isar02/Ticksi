using FluentValidation;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Events.Commands;

public abstract class EventInputValidator<T> : AbstractValidator<T> where T : EventInput
{
    protected EventInputValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(Event.Constraints.NameMaxLength)
            .WithMessage("Name cannot exceed {MaxLength} characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(Event.Constraints.DescriptionMaxLength)
            .WithMessage("Description cannot exceed {MaxLength} characters.");

        RuleFor(x => x.Contact)
            .NotEmpty().WithMessage("Contact is required.")
            .MaximumLength(Event.Constraints.ContactMaxLength)
            .WithMessage("Contact cannot exceed {MaxLength} characters.");

        RuleFor(x => x.Date)
            .GreaterThan(_ => timeProvider.GetUtcNow().UtcDateTime)
            .WithMessage("The event date must be in the future.");

        RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category is required.");
        RuleFor(x => x.EventTypeId).NotEmpty().WithMessage("Event type is required.");
        RuleFor(x => x.LocationId).NotEmpty().WithMessage("Venue is required.");
        RuleFor(x => x.OrganizerCompanyId).NotEmpty().WithMessage("Organizer company is required.");

        RuleFor(x => x.TicketTypes)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Add at least one ticket type.")
            .Must(HaveUniqueNames).WithMessage("Ticket type names must be unique.");

        RuleForEach(x => x.TicketTypes)
            .NotNull().WithMessage("Ticket type is required.")
            .SetValidator(new TicketTypeInputValidator());
    }

    private static bool HaveUniqueNames(List<TicketTypeInput> ticketTypes) =>
        ticketTypes
            .Select(t => t?.Name?.Trim() ?? string.Empty)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == ticketTypes.Count;

    private sealed class TicketTypeInputValidator : AbstractValidator<TicketTypeInput>
    {
        public TicketTypeInputValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Ticket type name is required.")
                .MaximumLength(TicketType.Constraints.NameMaxLength)
                .WithMessage("Ticket type name cannot exceed {MaxLength} characters.");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price cannot be negative.")
                .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("Price can have at most two decimals.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be at least 1.");
        }
    }
}
