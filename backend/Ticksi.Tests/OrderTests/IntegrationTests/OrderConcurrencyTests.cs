using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Orders.Commands.CreateOrder;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.OrderTests.IntegrationTests;

public class OrderConcurrencyTests(TicksiApiFactory factory) : IClassFixture<TicksiApiFactory>
{
    [Fact]
    public async Task Create_ConflictAfterAnotherLineWasSaved_UndoesThatLineBeforeRetrying()
    {
        var (buyer, item, connection) = await AddBuyerAndEventAsync();
        var vip = item.TicketTypes.Single(t => t.Name == "VIP");

        // The VIP row changes underneath, so the Standard row is already updated when the VIP update fails.
        var vipSoldElsewhere = new BeforeSaveInterceptor(1, async cancellationToken =>
        {
            await using var other = Context(connection);
            var ticketType = await other.TicketTypes.SingleAsync(t => t.Id == vip.Id, cancellationToken);
            ticketType.QuantityReserved += 1;
            await other.SaveChangesAsync(cancellationToken);
        });

        await using (var context = Context(connection, vipSoldElsewhere))
        {
            var handler = new CreateOrderCommandHandler(context, new FakeCurrentUser(buyer.PublicId), TimeProvider.System, EventClocks.Utc(TimeProvider.System));
            await handler.Handle(new CreateOrderCommand
            {
                Items =
                [
                    new() { TicketTypeId = item.TicketTypes.Single(t => t.Name == "Standard").PublicId, Quantity = 2 },
                    new() { TicketTypeId = vip.PublicId, Quantity = 1 }
                ]
            }, CancellationToken.None);
        }

        await using var verify = Context(connection);
        var reserved = await verify.TicketTypes
            .Where(t => t.EventId == item.Id)
            .OrderBy(t => t.Name)
            .Select(t => t.QuantityReserved)
            .ToListAsync();
        Assert.Equal([2, 2], reserved);
    }

    [Fact]
    public async Task Create_OrganizerLowersQuantityBelowRequested_ThrowsConflictAndReservesNothing()
    {
        var (buyer, item, connection) = await AddBuyerAndEventAsync();
        var vip = item.TicketTypes.Single(t => t.Name == "VIP");
        await using var context = Context(connection, ChangeQuantityBeforeSave(connection, vip.Id, 1));
        var handler = new CreateOrderCommandHandler(context, new FakeCurrentUser(buyer.PublicId), TimeProvider.System, EventClocks.Utc(TimeProvider.System));

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(OrderFor(item), CancellationToken.None));

        Assert.Equal("Only 1 \"VIP\" tickets are left for \"Concert\".", error.Message);
        await using var verify = Context(connection);
        Assert.Equal(1, (await verify.TicketTypes.SingleAsync(t => t.Id == vip.Id)).Quantity);
        Assert.All(await verify.TicketTypes.Where(t => t.EventId == item.Id).ToListAsync(),
            t => Assert.Equal(0, t.QuantityReserved));
        Assert.False(await verify.Orders.AnyAsync(o => o.AppUserId == buyer.Id));
    }

    [Fact]
    public async Task Create_OrganizerLowersQuantityWithEnoughLeft_RetriesAndReservesExactlyOnce()
    {
        var (buyer, item, connection) = await AddBuyerAndEventAsync();
        var vip = item.TicketTypes.Single(t => t.Name == "VIP");
        await using var context = Context(connection, ChangeQuantityBeforeSave(connection, vip.Id, 3));
        var handler = new CreateOrderCommandHandler(context, new FakeCurrentUser(buyer.PublicId), TimeProvider.System, EventClocks.Utc(TimeProvider.System));

        var order = await handler.Handle(OrderFor(item), CancellationToken.None);

        Assert.Equal(220m, order.TotalAmount);
        await using var verify = Context(connection);
        var ticketTypes = await verify.TicketTypes.Where(t => t.EventId == item.Id).ToListAsync();
        Assert.Equal(3, ticketTypes.Single(t => t.Id == vip.Id).Quantity);
        Assert.All(ticketTypes, t => Assert.Equal(2, t.QuantityReserved));
        Assert.Single(await verify.Orders.Where(o => o.AppUserId == buyer.Id).ToListAsync());
    }

    private async Task<(AppUser Buyer, Event Item, string Connection)> AddBuyerAndEventAsync()
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
            TicketTypes = [new() { Name = "Standard", Price = 30m, Quantity = 100 }, new() { Name = "VIP", Price = 80m, Quantity = 20 }]
        };
        database.Events.Add(item);
        await database.SaveChangesAsync();
        return (buyer, item, database.Database.GetConnectionString()!);
    }

    private static BeforeSaveInterceptor ChangeQuantityBeforeSave(string connection, int ticketTypeId, int quantity) =>
        new(1, async cancellationToken =>
        {
            await using var other = Context(connection);
            var ticketType = await other.TicketTypes.SingleAsync(t => t.Id == ticketTypeId, cancellationToken);
            ticketType.Quantity = quantity;
            await other.SaveChangesAsync(cancellationToken);
        });

    private static CreateOrderCommand OrderFor(Event item) => new()
    {
        Items = item.TicketTypes.Select(t => new OrderItemInput { TicketTypeId = t.PublicId, Quantity = 2 }).ToList()
    };

    private static AppDbContext Context(string connection, params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).AddInterceptors(interceptors).Options);
}
