using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;

namespace Ticksi.Tests.PaymentTests.UnitTests;

public class StartPaymentCommandHandlerTests : PaymentHandlerTestBase
{
    [Fact]
    public async Task Handle_PendingOrder_StartsACardPaymentTaggedForTicksi()
    {
        var (buyer, order) = await PlaceOrderAsync();

        var session = await StartAsync(buyer, order.PublicId);

        var request = Assert.Single(Gateway.Created);
        Assert.Equal((order.PublicId, "ticksi", 90m, "BAM"), (request.OrderId, request.Application, request.Amount, request.Currency));
        Assert.Equal($"Ticksi order #{order.PublicId.ToString("N")[..8].ToUpperInvariant()}", request.Description);
        Assert.False(session.Paid);
        Assert.Equal(("pk_test_fake", 90m, "BAM"), (session.PublishableKey, session.Amount, session.Currency));
        Assert.EndsWith("_secret", session.ClientSecret);

        var payment = (await FindOrderAsync(order.PublicId)).Payment!;
        Assert.Equal((PaymentStatus.Pending, 90m, "BAM", Now.UtcDateTime), (payment.Status, payment.Amount, payment.Currency, payment.CreatedAtUtc));
        Assert.StartsWith(payment.ProviderReference, session.ClientSecret);
    }

    [Fact]
    public async Task Handle_RepeatedRequest_ReusesTheSamePayment()
    {
        var (buyer, order) = await PlaceOrderAsync();

        var first = await StartAsync(buyer, order.PublicId);
        var second = await StartAsync(buyer, order.PublicId);

        Assert.Equal(1, Gateway.CreateCalls);
        Assert.Equal(first.ClientSecret, second.ClientSecret);
    }

    [Fact]
    public async Task Handle_AnotherUsersOrder_ThrowsNotFound()
    {
        var (_, order) = await PlaceOrderAsync();
        var stranger = await AddUserAsync(Role.Names.User);

        await Assert.ThrowsAsync<NotFoundException>(() => StartAsync(stranger, order.PublicId));
        Assert.Empty(Gateway.Created);
    }

    [Fact]
    public async Task Handle_PaymentThatAlreadySucceeded_SettlesTheOrderAndRefuses()
    {
        var (buyer, order) = await PlaceOrderAsync();
        await StartAsync(buyer, order.PublicId);
        Gateway.Report(await ReferenceOfAsync(order.PublicId), GatewayPaymentState.Succeeded);

        var error = await Assert.ThrowsAsync<ConflictException>(() => StartAsync(buyer, order.PublicId));

        Assert.Equal("This order is already paid.", error.Message);
        Assert.Equal(OrderStatus.Paid, (await FindOrderAsync(order.PublicId)).Status);
        await Assert.ThrowsAsync<ConflictException>(() => StartAsync(buyer, order.PublicId));
    }

    [Fact]
    public async Task Handle_OrderOfFreeTickets_IsPaidWithoutACardPayment()
    {
        var (buyer, order) = await PlaceOrderAsync(standard: 2, vip: 0, standardPrice: 0m);

        var session = await StartAsync(buyer, order.PublicId);

        Assert.True(session.Paid);
        Assert.Null(session.ClientSecret);
        Assert.Empty(Gateway.Created);
        var paid = await FindOrderAsync(order.PublicId);
        Assert.Equal((OrderStatus.Paid, 2), (paid.Status, paid.Items.Sum(i => i.Tickets.Count)));
        Assert.Null(paid.Payment);
    }

    [Fact]
    public async Task Handle_CardServiceDown_ReportsItAndStoresNothing()
    {
        var (buyer, order) = await PlaceOrderAsync();
        Gateway.Unavailable = true;

        await Assert.ThrowsAsync<PaymentGatewayException>(() => StartAsync(buyer, order.PublicId));
        Assert.Null((await FindOrderAsync(order.PublicId)).Payment);
    }
}
