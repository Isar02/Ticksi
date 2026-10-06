using FluentValidation;

namespace Ticksi.Application.Features.Payments.Commands.StartPayment;

public class StartPaymentCommandValidator : AbstractValidator<StartPaymentCommand>
{
    public StartPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order is required.");
    }
}
