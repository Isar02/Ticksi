using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;
using Ticksi.Domain.Enums;

namespace Ticksi.Application.Features.Payments;

internal static class OrderSettlement
{
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int CodeLength = 12;
    private const int MaxAttempts = 3;

    public static async Task ApplyAsync(
        IAppDbContext context,
        GatewayPayment reported,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .Include(p => p.Order!)
            .ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(p => p.ProviderReference == reported.Reference, cancellationToken);

        if (payment is null)
            return;

        for (var attempt = 1; ; attempt++)
        {
            var next = NextStatus(payment, reported);
            if (next is null)
                return;

            await using var transaction = await context.BeginTransactionAsync(cancellationToken);
            payment.Status = next.Value;
            payment.CompletedAtUtc = next == PaymentStatus.Succeeded ? nowUtc : null;

            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex) when (attempt < MaxAttempts)
            {
                // Another request changed the payment first; decide again from what it stored.
                await transaction.RollbackAsync(cancellationToken);
                foreach (var entry in ex.Entries)
                    await entry.ReloadAsync(cancellationToken);
                continue;
            }

            // Only the request that moved the payment to Succeeded issues the tickets, in the same transaction.
            if (next == PaymentStatus.Succeeded)
            {
                MarkPaid(payment.Order!, nowUtc);
                await context.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return;
        }
    }

    public static void MarkPaid(Order order, DateTime nowUtc)
    {
        order.Status = OrderStatus.Paid;
        order.PaidAtUtc = nowUtc;

        foreach (var item in order.Items)
        {
            for (var i = 0; i < item.Quantity; i++)
                item.Tickets.Add(new Ticket { Code = NewCode(), IssuedAtUtc = nowUtc });
        }
    }

    private static PaymentStatus? NextStatus(Payment payment, GatewayPayment reported)
    {
        if (payment.Status == PaymentStatus.Succeeded)
            return null;
        if (reported.State == GatewayPaymentState.Succeeded && Matches(payment, reported))
            return PaymentStatus.Succeeded;
        if (reported.State is GatewayPaymentState.Failed or GatewayPaymentState.Canceled && payment.Status != PaymentStatus.Failed)
            return PaymentStatus.Failed;
        return null;
    }

    private static bool Matches(Payment payment, GatewayPayment reported) =>
        reported.Amount == payment.Amount &&
        string.Equals(reported.Currency, payment.Currency, StringComparison.OrdinalIgnoreCase);

    private static string NewCode() => RandomNumberGenerator.GetString(CodeAlphabet, CodeLength);
}
