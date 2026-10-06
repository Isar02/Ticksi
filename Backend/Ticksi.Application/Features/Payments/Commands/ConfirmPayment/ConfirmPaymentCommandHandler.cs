using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Orders;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;

namespace Ticksi.Application.Features.Payments.Commands.ConfirmPayment;

// Asks the card processor directly, so a paid order completes even when the webhook is late.
public class ConfirmPaymentCommandHandler : IRequestHandler<ConfirmPaymentCommand, OrderDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IPaymentGateway _gateway;
    private readonly TimeProvider _timeProvider;

    public ConfirmPaymentCommandHandler(
        IAppDbContext context,
        ICurrentUser currentUser,
        IPaymentGateway gateway,
        TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _gateway = gateway;
        _timeProvider = timeProvider;
    }

    public async Task<OrderDto> Handle(ConfirmPaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await BuyerOrders.FindAsync(_context, _currentUser, request.OrderId, cancellationToken);

        if (order.Status == OrderStatus.Pending)
        {
            var payment = order.Payment ?? throw new ConflictException("The payment for this order has not been started.");
            var reported = await _gateway.GetAsync(payment.ProviderReference, cancellationToken);
            await OrderSettlement.ApplyAsync(_context, reported, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        }

        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == order.Id)
            .Select(OrderDto.Projection)
            .SingleAsync(cancellationToken);
    }
}
