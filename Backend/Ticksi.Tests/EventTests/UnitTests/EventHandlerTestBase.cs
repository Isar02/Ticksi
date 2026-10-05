using Ticksi.Application.Features.Events.Commands;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.EventTests.UnitTests;

public abstract class EventHandlerTestBase
{
    protected readonly InMemoryDatabase Database = new();
    protected readonly FakeFileStorage Files = new();

    protected static readonly DateTime NextYear = new(2027, 6, 1, 20, 0, 0, DateTimeKind.Utc);

    protected async Task<AppUser> AddUserAsync(string role, bool isActive = true)
    {
        await using var context = Database.CreateContext();

        var user = new AppUser
        {
            FirstName = "Lejla",
            LastName = "Begic",
            Email = $"{Guid.NewGuid():N}@ticksi.com",
            Phone = "+387 61 123 456",
            PasswordHash = "hash",
            IsActive = isActive,
            Role = await context.Roles.SingleAsync(r => r.Name == role)
        };

        context.AppUsers.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    protected async Task<References> AddReferencesAsync(int capacity = 1000)
    {
        await using var context = Database.CreateContext();

        var category = new EventCategory { Name = "Music" };
        var venue = new Location { Name = "Zetra", City = "Sarajevo", Address = "Alipašina bb", Capacity = capacity };
        var company = new OrganizerCompany { Name = "Ticksi Events", Email = "events@ticksi.com" };
        context.AddRange(category, venue, company);
        await context.SaveChangesAsync();

        var eventType = await context.EventTypes.FirstAsync();
        return new References(category.PublicId, eventType.PublicId, venue.PublicId, company.PublicId);
    }

    protected async Task<Event> AddEventAsync(
        AppUser owner,
        References references,
        int reserved = 0,
        string name = "Summer Concert",
        DateTime? date = null,
        string? posterUrl = null)
    {
        await using var context = Database.CreateContext();

        var item = new Event
        {
            Name = name,
            Description = "Open air.",
            Date = date ?? NextYear,
            Contact = "events@ticksi.com",
            PosterUrl = posterUrl,
            AppUserId = owner.Id,
            EventCategoryId = await IdOfAsync(context.EventCategories, references.CategoryId),
            EventTypeId = await IdOfAsync(context.EventTypes, references.EventTypeId),
            LocationId = await IdOfAsync(context.Locations, references.LocationId),
            OrganizerCompanyId = await IdOfAsync(context.OrganizerCompanies, references.OrganizerCompanyId),
            TicketTypes =
            [
                new() { Name = "Standard", Price = 20m, Quantity = 500, QuantityReserved = reserved },
                new() { Name = "VIP", Price = 50m, Quantity = 100 }
            ]
        };

        context.Events.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    protected async Task AddOrderAsync(
        AppUser buyer,
        Guid ticketTypePublicId,
        int quantity,
        OrderStatus status = OrderStatus.Paid)
    {
        await using var context = Database.CreateContext();
        var ticketType = await context.TicketTypes.SingleAsync(t => t.PublicId == ticketTypePublicId);

        context.Orders.Add(new Order
        {
            AppUserId = buyer.Id,
            Status = status,
            TotalAmount = ticketType.Price * quantity,
            Items = [new OrderItem { TicketTypeId = ticketType.Id, Quantity = quantity, UnitPrice = ticketType.Price }]
        });
        await context.SaveChangesAsync();
    }

    protected async Task<Event?> FindEventAsync(Guid publicId)
    {
        await using var context = Database.CreateContext();
        return await context.Events
            .Include(e => e.TicketTypes)
            .SingleOrDefaultAsync(e => e.PublicId == publicId);
    }

    protected static ICurrentUser SignedIn(AppUser user) => new FakeCurrentUser(user.PublicId);

    protected static T Input<T>(References references) where T : EventInput, new() => new()
    {
        Name = "  Winter Gala  ",
        Description = "An evening of music.",
        Date = NextYear,
        Contact = "gala@ticksi.com",
        CategoryId = references.CategoryId,
        EventTypeId = references.EventTypeId,
        LocationId = references.LocationId,
        OrganizerCompanyId = references.OrganizerCompanyId,
        TicketTypes =
        [
            new() { Name = "Standard", Price = 30m, Quantity = 800 },
            new() { Name = "VIP", Price = 75.5m, Quantity = 200 }
        ]
    };

    private static Task<int> IdOfAsync<T>(IQueryable<T> entities, Guid publicId) where T : BaseEntity =>
        entities.Where(e => e.PublicId == publicId).Select(e => e.Id).SingleAsync();

    protected sealed record References(Guid CategoryId, Guid EventTypeId, Guid LocationId, Guid OrganizerCompanyId);
}
