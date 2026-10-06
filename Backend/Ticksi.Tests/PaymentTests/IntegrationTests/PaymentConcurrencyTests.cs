using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.Features.Orders.Commands.CreateOrder;
using Ticksi.Application.Features.Payments.Commands.ConfirmPayment;
using Ticksi.Application.Features.Payments.Commands.HandlePaymentWebhook;
using Ticksi.Application.Features.Payments.Commands.StartPayment;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.PaymentTests.IntegrationTests;

public class PaymentConcurrencyTests(TicksiApiFactory factory) : IClassFixture<TicksiApiFactory>
{
    private readonly FakePaymentGateway _gateway = new();

    [Fact]
    public async Task Confirm_WhileTheWebhookSettlesTheSamePayment_IssuesTheTicketsOnce()
    {
        var (buyer, connection, orderId) = await StartedOrderAsync();
        var reported = _gateway.Report(await ReferenceOfAsync(connection, orderId), GatewayPaymentState.Succeeded);
        _gateway.Events["paid"] = new GatewayEvent(GatewayEventKind.PaymentSucceeded, reported);

        var webhookFirst = new BeforeSaveInterceptor(1, async cancellationToken =>
        {
            await using var other = Context(connection);
            await new HandlePaymentWebhookCommandHandler(other, _gateway, TimeProvider.System).Handle(
                new HandlePaymentWebhookCommand { Payload = "paid", Signature = FakePaymentGateway.ValidSignature }, cancellationToken);
        });

        await using (var context = Context(connection, webhookFirst))
        {
            var order = await new ConfirmPaymentCommandHandler(context, new FakeCurrentUser(buyer), _gateway, TimeProvider.System)
                .Handle(new ConfirmPaymentCommand(orderId), CancellationToken.None);
            Assert.Equal(OrderStatus.Paid, order.Status);
        }

        await using var verify = Context(connection);
        Assert.Equal(3, await verify.Tickets.CountAsync(t => t.OrderItem!.Order!.PublicId == orderId));
    }

    [Fact]
    public async Task Confirm_AfterAFailureWebhookWasSavedFirst_StillPaysTheOrder()
    {
        var (buyer, connection, orderId) = await StartedOrderAsync();
        var succeeded = _gateway.Report(await ReferenceOfAsync(connection, orderId), GatewayPaymentState.Succeeded);
        _gateway.Events["declined"] = new GatewayEvent(GatewayEventKind.PaymentFailed, succeeded with { State = GatewayPaymentState.Failed });

        var failureFirst = new BeforeSaveInterceptor(1, async cancellationToken =>
        {
            await using var other = Context(connection);
            await new HandlePaymentWebhookCommandHandler(other, _gateway, TimeProvider.System).Handle(
                new HandlePaymentWebhookCommand { Payload = "declined", Signature = FakePaymentGateway.ValidSignature }, cancellationToken);
        });

        await using (var context = Context(connection, failureFirst))
        {
            var order = await new ConfirmPaymentCommandHandler(context, new FakeCurrentUser(buyer), _gateway, TimeProvider.System)
                .Handle(new ConfirmPaymentCommand(orderId), CancellationToken.None);
            Assert.Equal(OrderStatus.Paid, order.Status);
        }

        await using var verify = Context(connection);
        Assert.Equal(PaymentStatus.Succeeded, await verify.Payments.Where(p => p.Order!.PublicId == orderId).Select(p => p.Status).SingleAsync());
        Assert.Equal(3, await verify.Tickets.CountAsync(t => t.OrderItem!.Order!.PublicId == orderId));
    }

    [Fact]
    public async Task Start_ParallelRequestsForAFreeOrder_IssueTheTicketsOnce()
    {
        var (buyer, connection, orderId) = await PlacedOrderAsync(price: 0m);
        var otherRequestFirst = new BeforeSaveInterceptor(1, async cancellationToken =>
        {
            await using var other = Context(connection);
            await new StartPaymentCommandHandler(other, new FakeCurrentUser(buyer), _gateway, TimeProvider.System)
                .Handle(new StartPaymentCommand(orderId), cancellationToken);
        });

        await using (var context = Context(connection, otherRequestFirst))
        {
            var session = await new StartPaymentCommandHandler(context, new FakeCurrentUser(buyer), _gateway, TimeProvider.System)
                .Handle(new StartPaymentCommand(orderId), CancellationToken.None);
            Assert.True(session.Paid);
        }

        await using var verify = Context(connection);
        Assert.Equal(3, await verify.Tickets.CountAsync(t => t.OrderItem!.Order!.PublicId == orderId));
        Assert.Empty(_gateway.Created);
    }

    private async Task<(Guid Buyer, string Connection, Guid OrderId)> StartedOrderAsync()
    {
        var (buyer, connection, orderId) = await PlacedOrderAsync(price: 30m);
        await using var context = Context(connection);
        await new StartPaymentCommandHandler(context, new FakeCurrentUser(buyer), _gateway, TimeProvider.System)
            .Handle(new StartPaymentCommand(orderId), CancellationToken.None);
        return (buyer, connection, orderId);
    }

    private async Task<(Guid Buyer, string Connection, Guid OrderId)> PlacedOrderAsync(decimal price)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var buyer = new AppUser
        {
            FirstName = "Lejla", LastName = "Begic", Email = $"buyer.{Guid.NewGuid():N}@ticksi.com",
            Phone = "+387 61 123 456", PasswordHash = "hash",
            Role = await database.Roles.SingleAsync(r => r.Name == Role.Names.User)
        };
        var item = new Event
        {
            Name = "Concert", Description = "Open air.", Date = DateTime.UtcNow.AddMonths(2), Contact = "events@ticksi.com",
            AppUser = buyer,
            EventCategory = new EventCategory { Name = "Music" },
            Location = new Location { Name = "Zetra", City = "Sarajevo", Address = "Alipašina bb", Capacity = 500 },
            OrganizerCompany = new OrganizerCompany { Name = "Ticksi Events", Email = "events@ticksi.com" },
            EventType = await database.EventTypes.FirstAsync(),
            TicketTypes = [new() { Name = "Standard", Price = price, Quantity = 100 }]
        };
        database.Events.Add(item);
        await database.SaveChangesAsync();

        var connection = database.Database.GetConnectionString()!;
        await using var context = Context(connection);
        var order = await new CreateOrderCommandHandler(context, new FakeCurrentUser(buyer.PublicId), TimeProvider.System).Handle(
            new CreateOrderCommand { Items = [new() { TicketTypeId = item.TicketTypes.Single().PublicId, Quantity = 3 }] },
            CancellationToken.None);

        return (buyer.PublicId, connection, order.PublicId);
    }

    private static async Task<string> ReferenceOfAsync(string connection, Guid orderId)
    {
        await using var context = Context(connection);
        return await context.Payments.Where(p => p.Order!.PublicId == orderId).Select(p => p.ProviderReference).SingleAsync();
    }

    private static AppDbContext Context(string connection, params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).AddInterceptors(interceptors).Options);
}
