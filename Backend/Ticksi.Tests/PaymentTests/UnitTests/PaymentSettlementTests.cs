using FluentValidation;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;

namespace Ticksi.Tests.PaymentTests.UnitTests;

public class PaymentSettlementTests : PaymentHandlerTestBase
{
    [Fact]
    public async Task Confirm_SucceededPayment_PaysTheOrderAndIssuesOneTicketPerSeat()
    {
        var (buyer, order) = await StartedOrderAsync();
        Gateway.Report(await ReferenceOfAsync(order.PublicId), GatewayPaymentState.Succeeded);

        var confirmed = await ConfirmAsync(buyer, order.PublicId);

        Assert.Equal(OrderStatus.Paid, confirmed.Status);
        var paid = await FindOrderAsync(order.PublicId);
        Assert.Equal(Now.UtcDateTime, paid.PaidAtUtc);
        Assert.Equal((PaymentStatus.Succeeded, Now.UtcDateTime), (paid.Payment!.Status, paid.Payment.CompletedAtUtc));
        Assert.Equal([2, 1], paid.Items.OrderBy(i => i.Id).Select(i => i.Tickets.Count));

        var tickets = paid.Items.SelectMany(i => i.Tickets).ToList();
        Assert.All(tickets, t => Assert.Matches("^[A-HJ-NP-Z2-9]{12}$", t.Code));
        Assert.All(tickets, t => Assert.Equal((TicketStatus.Valid, Now.UtcDateTime), (t.Status, t.IssuedAtUtc)));
        Assert.Equal(3, tickets.Select(t => t.Code).Distinct().Count());
    }

    [Fact]
    public async Task Confirm_Repeated_IssuesNoFurtherTickets()
    {
        var (buyer, order) = await StartedOrderAsync();
        Gateway.Report(await ReferenceOfAsync(order.PublicId), GatewayPaymentState.Succeeded);

        await ConfirmAsync(buyer, order.PublicId);
        await ConfirmAsync(buyer, order.PublicId);
        await ReceiveWebhookAsync(await SucceededEventAsync(order.PublicId));

        Assert.Equal(3, (await FindOrderAsync(order.PublicId)).Items.Sum(i => i.Tickets.Count));
    }

    [Fact]
    public async Task Confirm_DeclinedCard_KeepsTheOrderPendingForAnotherTry()
    {
        var (buyer, order) = await StartedOrderAsync();
        Gateway.Report(await ReferenceOfAsync(order.PublicId), GatewayPaymentState.Failed);

        var confirmed = await ConfirmAsync(buyer, order.PublicId);

        Assert.Equal(OrderStatus.Pending, confirmed.Status);
        var pending = await FindOrderAsync(order.PublicId);
        Assert.Equal(PaymentStatus.Failed, pending.Payment!.Status);
        Assert.Empty(pending.Items.SelectMany(i => i.Tickets));
    }

    [Fact]
    public async Task Confirm_ReportedAmountDiffers_DoesNotPay()
    {
        var (buyer, order) = await StartedOrderAsync();
        Gateway.Report(await ReferenceOfAsync(order.PublicId), GatewayPaymentState.Succeeded, amount: 1m);

        var confirmed = await ConfirmAsync(buyer, order.PublicId);

        Assert.Equal(OrderStatus.Pending, confirmed.Status);
        Assert.Empty((await FindOrderAsync(order.PublicId)).Items.SelectMany(i => i.Tickets));
    }

    [Fact]
    public async Task Confirm_PaymentNotStarted_ThrowsConflict()
    {
        var (buyer, order) = await PlaceOrderAsync();

        await Assert.ThrowsAsync<ConflictException>(() => ConfirmAsync(buyer, order.PublicId));
    }

    [Fact]
    public async Task Webhook_PaymentOfAnotherApplication_IsIgnored()
    {
        var (_, order) = await StartedOrderAsync();
        var reported = Gateway.Report(await ReferenceOfAsync(order.PublicId), GatewayPaymentState.Succeeded);
        Gateway.Events["foreign"] = new GatewayEvent(GatewayEventKind.PaymentSucceeded, reported with { Application = "other-app" });
        Gateway.Events["untagged"] = new GatewayEvent(GatewayEventKind.PaymentSucceeded, reported with { Application = null });
        Gateway.Events["other-kind"] = new GatewayEvent(GatewayEventKind.Other, reported);

        await ReceiveWebhookAsync("foreign");
        await ReceiveWebhookAsync("untagged");
        await ReceiveWebhookAsync("other-kind");

        Assert.Equal(OrderStatus.Pending, (await FindOrderAsync(order.PublicId)).Status);
    }

    [Fact]
    public async Task Webhook_SucceededPayment_PaysTheOrder()
    {
        var (_, order) = await StartedOrderAsync();

        await ReceiveWebhookAsync(await SucceededEventAsync(order.PublicId));

        Assert.Equal(OrderStatus.Paid, (await FindOrderAsync(order.PublicId)).Status);
    }

    [Fact]
    public async Task Webhook_FailureArrivingAfterTheSuccess_KeepsThePaidOrder()
    {
        var (buyer, order) = await StartedOrderAsync();
        var succeeded = Gateway.Report(await ReferenceOfAsync(order.PublicId), GatewayPaymentState.Succeeded);
        await ConfirmAsync(buyer, order.PublicId);
        Gateway.Events["late-failure"] = new GatewayEvent(GatewayEventKind.PaymentFailed, succeeded with { State = GatewayPaymentState.Failed });

        await ReceiveWebhookAsync("late-failure");

        var paid = await FindOrderAsync(order.PublicId);
        Assert.Equal((OrderStatus.Paid, PaymentStatus.Succeeded), (paid.Status, paid.Payment!.Status));
    }

    [Fact]
    public async Task Webhook_UnknownPayment_IsIgnored()
    {
        var unknown = new GatewayPayment("pi_unknown", "", GatewayPaymentState.Succeeded, 10m, "BAM", "ticksi", Guid.NewGuid());
        Gateway.Events["unknown"] = new GatewayEvent(GatewayEventKind.PaymentSucceeded, unknown);

        await ReceiveWebhookAsync("unknown");
    }

    [Fact]
    public async Task Webhook_BadSignature_IsRejected()
    {
        var (_, order) = await StartedOrderAsync();
        var payload = await SucceededEventAsync(order.PublicId);

        await Assert.ThrowsAsync<ValidationException>(() => ReceiveWebhookAsync(payload, "forged"));
        Assert.Equal(OrderStatus.Pending, (await FindOrderAsync(order.PublicId)).Status);
    }

    private async Task<(AppUser Buyer, Ticksi.Application.Features.Orders.OrderDto Order)> StartedOrderAsync()
    {
        var (buyer, order) = await PlaceOrderAsync();
        await StartAsync(buyer, order.PublicId);
        return (buyer, order);
    }

    private async Task<string> SucceededEventAsync(Guid orderId)
    {
        var reported = Gateway.Report(await ReferenceOfAsync(orderId), GatewayPaymentState.Succeeded);
        var payload = $"succeeded-{reported.Reference}";
        Gateway.Events[payload] = new GatewayEvent(GatewayEventKind.PaymentSucceeded, reported);
        return payload;
    }
}
