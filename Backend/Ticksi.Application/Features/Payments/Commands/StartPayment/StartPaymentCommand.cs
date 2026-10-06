using MediatR;

namespace Ticksi.Application.Features.Payments.Commands.StartPayment;

public record StartPaymentCommand(Guid OrderId) : IRequest<PaymentSessionDto>;
