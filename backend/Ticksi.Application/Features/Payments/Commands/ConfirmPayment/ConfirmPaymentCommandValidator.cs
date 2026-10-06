using FluentValidation;

namespace Ticksi.Application.Features.Payments.Commands.ConfirmPayment;

public class ConfirmPaymentCommandValidator : AbstractValidator<ConfirmPaymentCommand>
{
    public ConfirmPaymentCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("Order is required.");
    }
}
