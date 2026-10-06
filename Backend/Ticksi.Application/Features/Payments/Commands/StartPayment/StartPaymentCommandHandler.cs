using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;
using Ticksi.Domain.Enums;

namespace Ticksi.Application.Features.Payments.Commands.StartPayment;

public class StartPaymentCommandHandler : IRequestHandler<StartPaymentCommand, PaymentSessionDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IPaymentGateway _gateway;
    private readonly TimeProvider _timeProvider;

    public StartPaymentCommandHandler(
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

    public async Task<PaymentSessionDto> Handle(StartPaymentCommand request, CancellationToken cancellationToken)
    {
        var order = await BuyerOrders.FindAsync(_context, _currentUser, request.OrderId, cancellationToken);
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;

        EnsurePending(order);

        if (order.TotalAmount == 0)
        {
            await PayFreeOrderAsync(order, nowUtc, cancellationToken);
            return new PaymentSessionDto { Paid = true, Amount = 0 };
        }

        var reported = order.Payment is { } payment
            ? await _gateway.GetAsync(payment.ProviderReference, cancellationToken)
            : await CreateAsync(order, nowUtc, cancellationToken);

        if (reported.State == GatewayPaymentState.Succeeded)
        {
            await OrderSettlement.ApplyAsync(_context, reported, nowUtc, cancellationToken);
            throw new ConflictException("This order is already paid.");
        }

        if (reported.State == GatewayPaymentState.Canceled)
            throw new ConflictException("This payment was cancelled. Please reserve the tickets again.");

        return new PaymentSessionDto
        {
            ClientSecret = reported.ClientSecret,
            PublishableKey = _gateway.PublishableKey,
            Amount = order.TotalAmount
        };
    }

    private async Task<GatewayPayment> CreateAsync(Order order, DateTime nowUtc, CancellationToken cancellationToken)
    {
        // The same key returns the same card payment, so a double click cannot start two.
        var created = await _gateway.CreateAsync(new PaymentRequest(
            order.PublicId,
            PaymentDefaults.Application,
            $"Ticksi order #{PaymentDefaults.OrderNumber(order.PublicId)}",
            order.TotalAmount,
            PaymentDefaults.Currency,
            $"ticksi-order-{order.PublicId:N}"), cancellationToken);

        order.Payment = new Payment
        {
            ProviderReference = created.Reference,
            Amount = order.TotalAmount,
            Currency = PaymentDefaults.Currency,
            CreatedAtUtc = nowUtc
        };

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A parallel request may have stored the same payment first.
            if (!await _context.Payments.AnyAsync(p => p.ProviderReference == created.Reference, cancellationToken))
                throw;
        }

        return created;
    }

    private async Task PayFreeOrderAsync(Order order, DateTime nowUtc, CancellationToken cancellationToken)
    {
        OrderSettlement.MarkPaid(order, nowUtc);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // A parallel request issued the tickets first; the order's status token kept this one from issuing them again.
            foreach (var entry in ex.Entries)
                await entry.ReloadAsync(cancellationToken);
            if (order.Status != OrderStatus.Paid)
                throw;
        }
    }

    private static void EnsurePending(Order order)
    {
        if (order.Status == OrderStatus.Paid)
            throw new ConflictException("This order is already paid.");
        if (order.Status == OrderStatus.Cancelled)
            throw new ConflictException("This order was cancelled.");
    }
}
