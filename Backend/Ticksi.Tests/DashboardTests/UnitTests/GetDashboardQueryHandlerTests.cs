using Microsoft.Extensions.Time.Testing;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Dashboard;
using Ticksi.Application.Features.Dashboard.Queries.GetDashboard;
using Ticksi.Domain.Enums;
using Ticksi.Tests.EventTests.UnitTests;

namespace Ticksi.Tests.DashboardTests.UnitTests;

public class GetDashboardQueryHandlerTests : EventHandlerTestBase
{
    private static readonly DateTime Today = new(2026, 10, 6, 9, 0, 0, DateTimeKind.Utc);

    private readonly FakeTimeProvider _clock = new(new DateTimeOffset(Today));

    [Fact]
    public async Task Handle_Buyer_CountsUpcomingTicketsNextEventFavoritesAndLatestOrders()
    {
        var buyer = await AddUserAsync(Role.Names.User);
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var later = await AddEventAsync(organizer, references, name: "Winter Gala", date: NextYear.AddMonths(5));
        var sooner = await AddEventAsync(organizer, references, name: "Summer Concert");
        var past = await AddEventAsync(organizer, references, name: "Spring Fair", date: Today.AddMonths(-3));
        await AddOrderAsync(buyer, past, "Standard", 1, Today.AddMonths(-4));
        await AddOrderAsync(buyer, later, "VIP", 1, Today.AddDays(-3));
        var cancelledTicket = await AddOrderAsync(buyer, sooner, "Standard", 3, Today.AddDays(-2));
        await AddOrderAsync(buyer, sooner, "VIP", 2, Today.AddDays(-1), OrderStatus.Pending);
        await CancelOneTicketAsync(cancelledTicket);
        await AddFavoritesAsync(buyer, later, sooner);

        var dashboard = await HandleAsync(buyer);

        Assert.Equal(3, dashboard.UpcomingTickets);
        Assert.Equal(("Summer Concert", NextYear, "Zetra", "Sarajevo", 2),
            (dashboard.NextEvent!.Name, dashboard.NextEvent.Date, dashboard.NextEvent.VenueName, dashboard.NextEvent.VenueCity, dashboard.NextEvent.Tickets));
        Assert.Equal(sooner.PublicId, dashboard.NextEvent.EventId);
        Assert.Equal(2, dashboard.Favorites);
        Assert.Equal(
            [("Summer Concert", 2, 100m, OrderStatus.Pending), ("Summer Concert", 3, 60m, OrderStatus.Paid), ("Winter Gala", 1, 50m, OrderStatus.Paid)],
            dashboard.RecentOrders.Select(o => (string.Join(", ", o.EventNames), o.Tickets, o.TotalAmount, o.Status)));
        Assert.Null(dashboard.Sales);
    }

    [Fact]
    public async Task Handle_OrderAcrossEvents_NamesEachEventOnceInOrder()
    {
        var buyer = await AddUserAsync(Role.Names.User);
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var concert = await AddEventAsync(organizer, references, name: "Summer Concert");
        var gala = await AddEventAsync(organizer, references, name: "Winter Gala", date: NextYear.AddMonths(5));
        await AddOrderAcrossAsync(buyer, (concert, "VIP", 1), (gala, "Standard", 2), (concert, "Standard", 3));

        var order = Assert.Single((await HandleAsync(buyer)).RecentOrders);

        Assert.Equal(["Summer Concert", "Winter Gala"], order.EventNames);
        Assert.Equal((6, 150m), (order.Tickets, order.TotalAmount));
    }

    [Fact]
    public async Task Handle_Organizer_SumsPaidSalesOfOwnEventsOnly()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var other = await AddUserAsync(Role.Names.Organizer);
        var buyer = await AddUserAsync(Role.Names.User);
        var references = await AddReferencesAsync();
        var concert = await AddEventAsync(organizer, references, name: "Summer Concert");
        var gala = await AddEventAsync(organizer, references, name: "Winter Gala", date: NextYear.AddMonths(5));
        var fair = await AddEventAsync(organizer, references, name: "Spring Fair", date: Today.AddMonths(-3));
        var foreign = await AddEventAsync(other, references, name: "Derby Day");
        await AddOrderAsync(buyer, concert, "Standard", 4, Today.AddDays(-2));
        await AddOrderAsync(buyer, gala, "VIP", 2, Today.AddDays(-1));
        await AddOrderAsync(buyer, fair, "Standard", 1, Today.AddMonths(-4));
        await AddOrderAsync(buyer, gala, "VIP", 5, Today, OrderStatus.Pending);
        await AddOrderAsync(buyer, foreign, "VIP", 9, Today);

        var sales = (await HandleAsync(organizer)).Sales!;

        Assert.False(sales.AllEvents);
        Assert.Equal((2, 7, 200m), (sales.UpcomingEvents, sales.TicketsSold, sales.Revenue));
        Assert.Null(sales.ActiveUsers);
        Assert.Equal(
            [("Summer Concert", 4, 80m), ("Winter Gala", 2, 100m), ("Spring Fair", 1, 20m)],
            sales.TopEvents.Select(t => (t.Name, t.TicketsSold, t.Revenue)));
        Assert.Equal(concert.PublicId, sales.TopEvents[0].EventId);
    }

    [Fact]
    public async Task Handle_Admin_CoversEveryEventAndCountsActiveUsers()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var buyer = await AddUserAsync(Role.Names.User);
        await AddUserAsync(Role.Names.User, isActive: false);
        var concert = await AddEventAsync(organizer, await AddReferencesAsync());
        await AddOrderAsync(buyer, concert, "VIP", 3, Today);

        var sales = (await HandleAsync(admin)).Sales!;

        Assert.True(sales.AllEvents);
        Assert.Equal((1, 3, 150m, 3), (sales.UpcomingEvents, sales.TicketsSold, sales.Revenue, sales.ActiveUsers));
    }

    [Fact]
    public async Task Handle_NewOrganizer_ReturnsAnEmptyOverview()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);

        var dashboard = await HandleAsync(organizer);

        Assert.Equal((0, 0), (dashboard.UpcomingTickets, dashboard.Favorites));
        Assert.Null(dashboard.NextEvent);
        Assert.Empty(dashboard.RecentOrders);
        Assert.Equal((0, 0, 0m), (dashboard.Sales!.UpcomingEvents, dashboard.Sales.TicketsSold, dashboard.Sales.Revenue));
        Assert.Empty(dashboard.Sales.TopEvents);
    }

    [Fact]
    public async Task Handle_DeactivatedAccount_ThrowsUnauthorized()
    {
        var user = await AddUserAsync(Role.Names.User, isActive: false);

        await Assert.ThrowsAsync<UnauthorizedException>(() => HandleAsync(user));
    }

    private async Task<int> AddOrderAsync(
        AppUser buyer,
        Event item,
        string ticketTypeName,
        int quantity,
        DateTime createdAtUtc,
        OrderStatus status = OrderStatus.Paid)
    {
        await using var context = Database.CreateContext();
        var ticketType = await context.TicketTypes.SingleAsync(t => t.EventId == item.Id && t.Name == ticketTypeName);
        var orderItem = new OrderItem { TicketTypeId = ticketType.Id, Quantity = quantity, UnitPrice = ticketType.Price };
        if (status == OrderStatus.Paid)
        {
            for (var i = 0; i < quantity; i++)
                orderItem.Tickets.Add(new Ticket { Code = Guid.NewGuid().ToString("N")[..12], IssuedAtUtc = createdAtUtc });
        }

        var order = new Order
        {
            AppUserId = buyer.Id,
            Status = status,
            TotalAmount = ticketType.Price * quantity,
            CreatedAtUtc = createdAtUtc,
            PaidAtUtc = status == OrderStatus.Paid ? createdAtUtc : null,
            Items = [orderItem]
        };
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order.Id;
    }

    private async Task AddOrderAcrossAsync(AppUser buyer, params (Event Event, string TicketType, int Quantity)[] lines)
    {
        await using var context = Database.CreateContext();
        var items = new List<OrderItem>();
        foreach (var (item, ticketTypeName, quantity) in lines)
        {
            var ticketType = await context.TicketTypes.SingleAsync(t => t.EventId == item.Id && t.Name == ticketTypeName);
            items.Add(new OrderItem { TicketTypeId = ticketType.Id, Quantity = quantity, UnitPrice = ticketType.Price });
        }

        context.Orders.Add(new Order
        {
            AppUserId = buyer.Id,
            Status = OrderStatus.Pending,
            TotalAmount = items.Sum(i => i.Quantity * i.UnitPrice),
            CreatedAtUtc = Today,
            Items = items
        });
        await context.SaveChangesAsync();
    }

    private async Task CancelOneTicketAsync(int orderId)
    {
        await using var context = Database.CreateContext();
        var ticket = await context.Tickets.FirstAsync(t => t.OrderItem!.OrderId == orderId);
        ticket.Status = TicketStatus.Cancelled;
        await context.SaveChangesAsync();
    }

    private async Task AddFavoritesAsync(AppUser user, params Event[] events)
    {
        await using var context = Database.CreateContext();
        context.Favorites.AddRange(events.Select(e => new Favorite { AppUserId = user.Id, EventId = e.Id }));
        await context.SaveChangesAsync();
    }

    private async Task<DashboardDto> HandleAsync(AppUser user)
    {
        await using var context = Database.CreateContext();
        return await new GetDashboardQueryHandler(context, SignedIn(user), _clock).Handle(new GetDashboardQuery(), CancellationToken.None);
    }
}
