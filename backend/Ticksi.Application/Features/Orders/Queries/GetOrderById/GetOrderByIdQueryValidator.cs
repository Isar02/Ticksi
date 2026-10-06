using FluentValidation;

namespace Ticksi.Application.Features.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryValidator : AbstractValidator<GetOrderByIdQuery>
{
    public GetOrderByIdQueryValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order is required.");
    }
}
