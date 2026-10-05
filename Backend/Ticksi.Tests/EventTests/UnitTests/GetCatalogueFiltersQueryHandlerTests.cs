using Ticksi.Application.Features.Events.Queries.GetCatalogueFilters;

namespace Ticksi.Tests.EventTests.UnitTests;

public class GetCatalogueFiltersQueryHandlerTests : EventHandlerTestBase
{
    [Fact]
    public async Task Handle_ListsCategoriesAndCitiesThatHaveEventsByName()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var references = await AddReferencesAsync();
        var theatre = await AddAsync(new EventCategory { Name = "Theatre" });
        await AddAsync(new EventCategory { Name = "Art" });
        var mostar = await AddAsync(Venue("Mostar"));
        var secondInSarajevo = await AddAsync(Venue("Sarajevo"));
        await AddAsync(Venue("Tuzla"));

        await AddEventAsync(organizer, references);
        await AddEventAsync(organizer, references with { CategoryId = theatre, LocationId = mostar });
        await AddEventAsync(organizer, references with { LocationId = secondInSarajevo });

        await using var context = Database.CreateContext();
        var filters = await new GetCatalogueFiltersQueryHandler(context)
            .Handle(new GetCatalogueFiltersQuery(), CancellationToken.None);

        Assert.Equal([("Music", references.CategoryId), ("Theatre", theatre)], filters.Categories.Select(c => (c.Name, c.PublicId)));
        Assert.Equal(["Mostar", "Sarajevo"], filters.Cities);
    }

    private static Location Venue(string city) =>
        new() { Name = $"{city} Arena", City = city, Address = "Bulevar 1", Capacity = 3000 };

    private async Task<Guid> AddAsync(BaseEntity entity)
    {
        await using var context = Database.CreateContext();
        context.Add(entity);
        await context.SaveChangesAsync();
        return entity.PublicId;
    }
}
