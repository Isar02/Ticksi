using Ticksi.Application.Features.Events.Queries.GetEventFormOptions;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.EventTests.UnitTests;

public class GetEventFormOptionsQueryHandlerTests
{
    private readonly InMemoryDatabase _database = new();

    [Fact]
    public async Task Handle_ReturnsVenuesEventTypesAndCompaniesByName()
    {
        await using (var context = _database.CreateContext())
        {
            context.AddRange(
                new Location { Name = "Zetra", City = "Sarajevo", Address = "Alipašina bb", Capacity = 12000 },
                new Location { Name = "Arena Mostar", City = "Mostar", Address = "Bulevar 1", Capacity = 3000 },
                new OrganizerCompany { Name = "Sound Works", Email = "info@sound.ba" },
                new OrganizerCompany { Name = "Ticksi Events", Email = "events@ticksi.com" });
            await context.SaveChangesAsync();
        }

        await using var queryContext = _database.CreateContext();
        var options = await new GetEventFormOptionsQueryHandler(queryContext)
            .Handle(new GetEventFormOptionsQuery(), CancellationToken.None);

        Assert.Equal(
            [("Arena Mostar", "Mostar", 3000), ("Zetra", "Sarajevo", 12000)],
            options.Venues.Select(v => (v.Name, v.City, v.Capacity)));
        Assert.Equal(["Sound Works", "Ticksi Events"], options.OrganizerCompanies.Select(c => c.Name));
        Assert.NotEmpty(options.EventTypes);
        Assert.Equal(options.EventTypes.Select(t => t.Name).Order(StringComparer.Ordinal), options.EventTypes.Select(t => t.Name));
    }
}
