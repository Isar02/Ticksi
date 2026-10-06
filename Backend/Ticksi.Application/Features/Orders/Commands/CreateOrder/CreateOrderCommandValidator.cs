using FluentValidation;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.Items)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Choose at least one ticket.")
            .Must(HaveUniqueTicketTypes).WithMessage("Each ticket type can appear only once.");

        RuleForEach(x => x.Items)
            .NotNull().WithMessage("Ticket is required.")
            .SetValidator(new OrderItemInputValidator());
    }

    private static bool HaveUniqueTicketTypes(List<OrderItemInput> items) =>
        items.Select(i => i?.TicketTypeId).Distinct().Count() == items.Count;

    private sealed class OrderItemInputValidator : AbstractValidator<OrderItemInput>
    {
        public OrderItemInputValidator()
        {
            RuleFor(x => x.TicketTypeId).NotEmpty().WithMessage("Ticket type is required.");

            RuleFor(x => x.Quantity)
                .InclusiveBetween(1, OrderItem.Constraints.MaxQuantity)
                .WithMessage("Quantity must be between {From} and {To}.");
        }
    }
}
