using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Favorites.Queries.GetFavoriteEvents;
using Ticksi.Tests.Common;
using Ticksi.Tests.EventTests.UnitTests;

namespace Ticksi.Tests.FavoriteTests.UnitTests;

public class GetFavoriteEventsQueryHandlerTests : EventHandlerTestBase
{
    [Fact]
    public async Task Handle_ReturnsOwnFavoritesWithCardDataNewestFirst()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var visitor = await AddUserAsync(Role.Names.User);
        var other = await AddUserAsync(Role.Names.User);
        var references = await AddReferencesAsync();
        var jazz = await AddEventAsync(organizer, references, name: "Jazz", reserved: 20);
        var opera = await AddEventAsync(organizer, references, name: "Opera");
        var derby = await AddEventAsync(organizer, references, name: "Derby");
        await AddFavoritesAsync((visitor, jazz), (other, opera), (visitor, derby), (visitor, opera));

        var favorites = await HandleAsync(new FakeCurrentUser(visitor.PublicId));

        Assert.Equal(["Opera", "Derby", "Jazz"], favorites.Select(e => e.Name));
        var first = favorites[^1];
        Assert.Equal((jazz.PublicId, 20m, 580, "Music", "Zetra"),
            (first.PublicId, first.LowestPrice!.Value, first.AvailableTickets, first.EventCategoryName, first.LocationName));
    }

    [Fact]
    public async Task Handle_NoFavorites_ReturnsAnEmptyList()
    {
        var visitor = await AddUserAsync(Role.Names.User);

        Assert.Empty(await HandleAsync(new FakeCurrentUser(visitor.PublicId)));
    }

    [Fact]
    public async Task Handle_WithoutUser_ThrowsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => HandleAsync(new FakeCurrentUser(null)));
    }

    private async Task AddFavoritesAsync(params (AppUser User, Event Event)[] favorites)
    {
        foreach (var (user, item) in favorites)
        {
            await using var context = Database.CreateContext();
            context.Favorites.Add(new Favorite { AppUserId = user.Id, EventId = item.Id });
            await context.SaveChangesAsync();
        }
    }

    private async Task<List<EventReadDto>> HandleAsync(FakeCurrentUser currentUser)
    {
        await using var context = Database.CreateContext();
        return await new GetFavoriteEventsQueryHandler(context, currentUser).Handle(new GetFavoriteEventsQuery(), CancellationToken.None);
    }
}
