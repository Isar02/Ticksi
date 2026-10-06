using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.DTOs;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.FavoriteTests.IntegrationTests;

public class FavoriteApiTests(TicksiApiFactory factory) : IClassFixture<TicksiApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetEvents_ListsOwnFavoritesNewestFirst()
    {
        var token = await RegisterAsync();
        var otherToken = await RegisterAsync();
        var first = await AddEventAsync("First");
        var second = await AddEventAsync("Second");
        await FavoriteAsync(token, first);
        await FavoriteAsync(otherToken, first);
        await FavoriteAsync(token, second);

        var response = await SendAsync(HttpMethod.Get, "/api/favorites/events", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = (await response.Content.ReadFromJsonAsync<List<EventReadDto>>())!;
        Assert.Equal([second, first], events.Select(e => e.PublicId));
        Assert.Equal(("Second", 30m, "Zetra"), (events[0].Name, events[0].LowestPrice!.Value, events[0].LocationName));
    }

    [Fact]
    public async Task GetEvents_AfterRemovingAFavorite_NoLongerListsIt()
    {
        var token = await RegisterAsync();
        var item = await AddEventAsync("Removed");
        await FavoriteAsync(token, item);

        var removed = await SendAsync(HttpMethod.Delete, $"/api/favorites/{item}", token);
        var events = await (await SendAsync(HttpMethod.Get, "/api/favorites/events", token))
            .Content.ReadFromJsonAsync<List<EventReadDto>>();

        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Empty(events!);
    }

    [Fact]
    public async Task GetEvents_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/favorites/events");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<string> RegisterAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            FirstName = "Lejla",
            LastName = "Begic",
            Email = $"visitor.{Guid.NewGuid():N}@ticksi.com",
            Password = "Secret123",
            Phone = "+387 61 123 456"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!.AccessToken;
    }

    private async Task<Guid> AddEventAsync(string name)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var item = new Event
        {
            Name = name,
            Description = "Open air.",
            Date = DateTime.UtcNow.AddMonths(2),
            Contact = "events@ticksi.com",
            AppUser = await context.AppUsers.FirstAsync(),
            EventCategory = new EventCategory { Name = "Music" },
            Location = new Location { Name = "Zetra", City = "Sarajevo", Address = "Alipašina bb", Capacity = 500 },
            OrganizerCompany = new OrganizerCompany { Name = "Ticksi Events", Email = "events@ticksi.com" },
            EventType = await context.EventTypes.FirstAsync(),
            TicketTypes = [new() { Name = "Standard", Price = 30m, Quantity = 100 }]
        };

        context.Events.Add(item);
        await context.SaveChangesAsync();
        return item.PublicId;
    }

    private async Task FavoriteAsync(string token, Guid eventPublicId)
    {
        var response = await SendAsync(HttpMethod.Post, $"/api/favorites/{eventPublicId}", token);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }
}
