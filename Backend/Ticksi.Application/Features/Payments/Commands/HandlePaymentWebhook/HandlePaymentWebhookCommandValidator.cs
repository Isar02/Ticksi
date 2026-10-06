using FluentValidation;

namespace Ticksi.Application.Features.Payments.Commands.HandlePaymentWebhook;

public class HandlePaymentWebhookCommandValidator : AbstractValidator<HandlePaymentWebhookCommand>
{
    public HandlePaymentWebhookCommandValidator()
    {
        RuleFor(x => x.Payload).NotEmpty().WithMessage("The webhook body is empty.");
        RuleFor(x => x.Signature).NotEmpty().WithMessage("The webhook signature is missing.");
    }
}
