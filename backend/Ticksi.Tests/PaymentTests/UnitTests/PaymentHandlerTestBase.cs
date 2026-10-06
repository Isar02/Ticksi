using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Time.Testing;
using Ticksi.Application.Features.Orders;
using Ticksi.Application.Features.Orders.Commands.CreateOrder;
using Ticksi.Application.Features.Payments;
using Ticksi.Application.Features.Payments.Commands.ConfirmPayment;
using Ticksi.Application.Features.Payments.Commands.HandlePaymentWebhook;
using Ticksi.Application.Features.Payments.Commands.StartPayment;
using Ticksi.Tests.Common;
using Ticksi.Tests.EventTests.UnitTests;

namespace Ticksi.Tests.PaymentTests.UnitTests;

public abstract class PaymentHandlerTestBase : EventHandlerTestBase
{
    protected static readonly DateTimeOffset Now = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    protected readonly FakePaymentGateway Gateway = new();
    private readonly FakeTimeProvider _time = new(Now);

    protected async Task<(AppUser Buyer, OrderDto Order)> PlaceOrderAsync(int standard = 2, int vip = 1, decimal? standardPrice = null)
    {
        var buyer = await AddUserAsync(Role.Names.User);
        var item = await AddEventAsync(await AddUserAsync(Role.Names.Organizer), await AddReferencesAsync());

        await using var context = Database.CreateContext();
        if (standardPrice is { } price)
        {
            var standardType = await context.TicketTypes.SingleAsync(t => t.EventId == item.Id && t.Name == "Standard");
            standardType.Price = price;
            await context.SaveChangesAsync();
        }

        var lines = new List<OrderItemInput>();
        if (standard > 0) lines.Add(new() { TicketTypeId = TicketTypeId(item, "Standard"), Quantity = standard });
        if (vip > 0) lines.Add(new() { TicketTypeId = TicketTypeId(item, "VIP"), Quantity = vip });

        var order = await new CreateOrderCommandHandler(context, SignedIn(buyer), _time, EventClocks.Utc(_time))
            .Handle(new CreateOrderCommand { Items = lines }, CancellationToken.None);
        return (buyer, order);
    }

    protected async Task<PaymentSessionDto> StartAsync(AppUser user, Guid orderId)
    {
        await using var context = Database.CreateContext();
        return await new StartPaymentCommandHandler(context, SignedIn(user), Gateway, _time)
            .Handle(new StartPaymentCommand(orderId), CancellationToken.None);
    }

    protected async Task<OrderDto> ConfirmAsync(AppUser user, Guid orderId, params IInterceptor[] interceptors)
    {
        await using var context = Database.CreateContext(interceptors);
        return await new ConfirmPaymentCommandHandler(context, SignedIn(user), Gateway, _time)
            .Handle(new ConfirmPaymentCommand(orderId), CancellationToken.None);
    }

    protected async Task ReceiveWebhookAsync(string payload, string signature = FakePaymentGateway.ValidSignature)
    {
        await using var context = Database.CreateContext();
        await new HandlePaymentWebhookCommandHandler(context, Gateway, _time)
            .Handle(new HandlePaymentWebhookCommand { Payload = payload, Signature = signature }, CancellationToken.None);
    }

    protected async Task<Order> FindOrderAsync(Guid orderId)
    {
        await using var context = Database.CreateContext();
        return await context.Orders
            .Include(o => o.Payment)
            .Include(o => o.Items).ThenInclude(i => i.Tickets)
            .SingleAsync(o => o.PublicId == orderId);
    }

    protected async Task<string> ReferenceOfAsync(Guid orderId) => (await FindOrderAsync(orderId)).Payment!.ProviderReference;

    private static Guid TicketTypeId(Event item, string name) => item.TicketTypes.Single(t => t.Name == name).PublicId;
}
