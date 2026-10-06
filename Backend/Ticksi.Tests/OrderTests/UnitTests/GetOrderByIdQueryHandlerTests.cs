using Microsoft.Extensions.Time.Testing;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Orders;
using Ticksi.Application.Features.Orders.Commands.CreateOrder;
using Ticksi.Application.Features.Orders.Queries.GetOrderById;
using Ticksi.Domain.Enums;
using Ticksi.Tests.EventTests.UnitTests;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.OrderTests.UnitTests;

public class GetOrderByIdQueryHandlerTests : EventHandlerTestBase
{
    [Fact]
    public async Task Handle_OwnOrder_ReturnsItWithItsItems()
    {
        var buyer = await AddUserAsync(Role.Names.User);
        var item = await AddEventAsync(await AddUserAsync(Role.Names.Organizer), await AddReferencesAsync());
        var created = await CreateOrderAsync(buyer, item);

        var order = await GetAsync(buyer, created.PublicId);

        Assert.Equal((OrderStatus.Pending, 100m), (order.Status, order.TotalAmount));
        var line = Assert.Single(order.Items);
        Assert.Equal(("Summer Concert", NextYear, "VIP", 2, 50m),
            (line.EventName, line.EventDate, line.TicketTypeName, line.Quantity, line.UnitPrice));
    }

    [Fact]
    public async Task Handle_AnotherUsersOrder_ThrowsNotFound()
    {
        var buyer = await AddUserAsync(Role.Names.User);
        var other = await AddUserAsync(Role.Names.User);
        var item = await AddEventAsync(await AddUserAsync(Role.Names.Organizer), await AddReferencesAsync());
        var created = await CreateOrderAsync(buyer, item);

        await Assert.ThrowsAsync<NotFoundException>(() => GetAsync(other, created.PublicId));
    }

    private async Task<OrderDto> CreateOrderAsync(AppUser buyer, Event item)
    {
        await using var context = Database.CreateContext();
        var handler = new CreateOrderCommandHandler(context, SignedIn(buyer), new FakeTimeProvider(NextYear.AddMonths(-1)), EventClocks.Utc(new FakeTimeProvider(NextYear.AddMonths(-1))));
        var vip = item.TicketTypes.Single(t => t.Name == "VIP").PublicId;
        return await handler.Handle(
            new CreateOrderCommand { Items = [new() { TicketTypeId = vip, Quantity = 2 }] },
            CancellationToken.None);
    }

    private async Task<OrderDto> GetAsync(AppUser user, Guid orderId)
    {
        await using var context = Database.CreateContext();
        return await new GetOrderByIdQueryHandler(context, SignedIn(user))
            .Handle(new GetOrderByIdQuery(orderId), CancellationToken.None);
    }
}
