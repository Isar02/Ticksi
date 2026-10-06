using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Time.Testing;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Orders;
using Ticksi.Application.Features.Orders.Commands.CreateOrder;
using Ticksi.Domain.Enums;
using Ticksi.Tests.Common;
using Ticksi.Tests.EventTests.UnitTests;

namespace Ticksi.Tests.OrderTests.UnitTests;

public class CreateOrderCommandHandlerTests : EventHandlerTestBase
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_AvailableTickets_CreatesPendingOrderAtCurrentPricesAndReservesThem()
    {
        var (buyer, item) = await AddBuyerAndEventAsync(reserved: 10);

        var order = await CreateAsync(buyer, Line(item, "Standard", 3), Line(item, "VIP", 2));

        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(160m, order.TotalAmount);
        Assert.Equal(Now.UtcDateTime, order.CreatedAtUtc);
        Assert.Equal(
            [("Standard", 3, 20m), ("VIP", 2, 50m)],
            order.Items.Select(i => (i.TicketTypeName, i.Quantity, i.UnitPrice)));
        Assert.All(order.Items, i => Assert.Equal((item.PublicId, "Summer Concert"), (i.EventId, i.EventName)));

        Assert.Equal((13, 2), await ReservedAsync(item));
        await using var context = Database.CreateContext();
        Assert.Equal(buyer.Id, (await context.Orders.SingleAsync(o => o.PublicId == order.PublicId)).AppUserId);
    }

    [Fact]
    public async Task Handle_TooFewTicketsLeft_ThrowsConflictAndReservesNothing()
    {
        var (buyer, item) = await AddBuyerAndEventAsync(reserved: 497);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateAsync(buyer, Line(item, "VIP", 1), Line(item, "Standard", 4)));

        Assert.Equal("Only 3 \"Standard\" tickets are left for \"Summer Concert\".", error.Message);
        Assert.Equal((497, 0), await ReservedAsync(item));
        Assert.False(await AnyOrderAsync());
    }

    [Fact]
    public async Task Handle_SoldOutTicketType_SaysItIsSoldOut()
    {
        var (buyer, item) = await AddBuyerAndEventAsync(reserved: 500);

        var error = await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(buyer, Line(item, "Standard", 1)));

        Assert.Equal("\"Standard\" tickets for \"Summer Concert\" are sold out.", error.Message);
    }

    [Fact]
    public async Task Handle_EventAlreadyStarted_ThrowsConflict()
    {
        var buyer = await AddUserAsync(Role.Names.User);
        var item = await AddEventAsync(buyer, await AddReferencesAsync(), date: Now.UtcDateTime);

        var error = await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(buyer, Line(item, "Standard", 1)));

        Assert.Equal("Ticket sales for \"Summer Concert\" have closed.", error.Message);
        Assert.False(await AnyOrderAsync());
    }

    [Fact]
    public async Task Handle_EventStartedInItsTimeZone_ThrowsConflictThoughLaterInUtc()
    {
        var buyer = await AddUserAsync(Role.Names.User);
        var item = await AddEventAsync(buyer, await AddReferencesAsync(), date: new DateTime(2026, 10, 6, 13, 30, 0));
        _timeZone = "Europe/Sarajevo";

        await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(buyer, Line(item, "Standard", 1)));

        Assert.False(await AnyOrderAsync());
    }

    [Fact]
    public async Task Handle_UnknownTicketType_ThrowsNotFound()
    {
        var (buyer, item) = await AddBuyerAndEventAsync();
        var unknown = new OrderItemInput { TicketTypeId = Guid.NewGuid(), Quantity = 1 };

        await Assert.ThrowsAsync<NotFoundException>(() => CreateAsync(buyer, Line(item, "Standard", 1), unknown));
        Assert.Equal((0, 0), await ReservedAsync(item));
    }

    [Fact]
    public async Task Handle_UnknownAccount_ThrowsUnauthorized()
    {
        var (_, item) = await AddBuyerAndEventAsync();
        var stranger = new AppUser { PublicId = Guid.NewGuid() };

        await Assert.ThrowsAsync<UnauthorizedException>(() => CreateAsync(stranger, Line(item, "Standard", 1)));
    }

    [Fact]
    public async Task Handle_ParallelPurchase_RetriesWithTheStoredCount()
    {
        var (buyer, item) = await AddBuyerAndEventAsync(reserved: 490);

        var order = await CreateAsync(buyer, [ReservedElsewhere(item, times: 1, quantity: 4)],
            Line(item, "Standard", 5), Line(item, "VIP", 1));

        Assert.Equal(6, order.Items.Sum(i => i.Quantity));
        Assert.Equal((499, 1), await ReservedAsync(item));
    }

    [Fact]
    public async Task Handle_ParallelPurchaseTakesTheLastTickets_ThrowsConflict()
    {
        var (buyer, item) = await AddBuyerAndEventAsync(reserved: 495);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateAsync(buyer, [ReservedElsewhere(item, times: 1, quantity: 4)], Line(item, "Standard", 3)));

        Assert.Equal("Only 1 \"Standard\" tickets are left for \"Summer Concert\".", error.Message);
        Assert.Equal((499, 0), await ReservedAsync(item));
        Assert.False(await AnyOrderAsync());
    }

    [Fact]
    public async Task Handle_ConflictOnEveryAttempt_GivesUpWithConflict()
    {
        var (buyer, item) = await AddBuyerAndEventAsync();

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            CreateAsync(buyer, [ReservedElsewhere(item, times: 3, quantity: 1)], Line(item, "Standard", 2)));

        Assert.Equal("Tickets are selling fast right now. Please try again.", error.Message);
        Assert.Equal((3, 0), await ReservedAsync(item));
        Assert.False(await AnyOrderAsync());
    }

    private async Task<(AppUser Buyer, Event Item)> AddBuyerAndEventAsync(int reserved = 0)
    {
        var buyer = await AddUserAsync(Role.Names.User);
        var organizer = await AddUserAsync(Role.Names.Organizer);
        return (buyer, await AddEventAsync(organizer, await AddReferencesAsync(), reserved));
    }

    private static OrderItemInput Line(Event item, string ticketTypeName, int quantity) => new()
    {
        TicketTypeId = item.TicketTypes.Single(t => t.Name == ticketTypeName).PublicId,
        Quantity = quantity
    };

    private BeforeSaveInterceptor ReservedElsewhere(Event item, int times, int quantity)
    {
        var standardId = item.TicketTypes.Single(t => t.Name == "Standard").PublicId;

        return new BeforeSaveInterceptor(times, async cancellationToken =>
        {
            await using var other = Database.CreateContext();
            var standard = await other.TicketTypes.SingleAsync(t => t.PublicId == standardId, cancellationToken);
            standard.QuantityReserved += quantity;
            await other.SaveChangesAsync(cancellationToken);
        });
    }

    private async Task<(int Standard, int Vip)> ReservedAsync(Event item)
    {
        await using var context = Database.CreateContext();
        var reserved = await context.TicketTypes
            .Where(t => t.Event!.PublicId == item.PublicId)
            .ToDictionaryAsync(t => t.Name, t => t.QuantityReserved);
        return (reserved["Standard"], reserved["VIP"]);
    }

    private async Task<bool> AnyOrderAsync()
    {
        await using var context = Database.CreateContext();
        return await context.Orders.AnyAsync();
    }

    private string _timeZone = "UTC";

    private Task<OrderDto> CreateAsync(AppUser buyer, params OrderItemInput[] items) => CreateAsync(buyer, [], items);

    private async Task<OrderDto> CreateAsync(AppUser buyer, IInterceptor[] interceptors, params OrderItemInput[] items)
    {
        await using var context = Database.CreateContext(interceptors);
        var handler = new CreateOrderCommandHandler(context, SignedIn(buyer), new FakeTimeProvider(Now), EventClocks.In(_timeZone, new FakeTimeProvider(Now)));
        return await handler.Handle(new CreateOrderCommand { Items = [.. items] }, CancellationToken.None);
    }
}
