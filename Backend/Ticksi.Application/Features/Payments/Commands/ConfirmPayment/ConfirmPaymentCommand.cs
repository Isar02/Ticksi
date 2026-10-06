using MediatR;
using Ticksi.Application.Features.Orders;

namespace Ticksi.Application.Features.Payments.Commands.ConfirmPayment;

public record ConfirmPaymentCommand(Guid OrderId) : IRequest<OrderDto>;
