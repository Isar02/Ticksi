using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.DTOs;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.EventTests.IntegrationTests;

public class EventApiTests(TicksiApiFactory factory) : IClassFixture<TicksiApiFactory>
{
    private const string Password = "Secret123";
    private static readonly Guid ConcertTypeId = Guid.Parse("4b7e9a10-2c5d-4e8f-a1b3-0d6c8e2f4a01");

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_AsOrganizer_Returns201AndTheEventCanBeRead()
    {
        var token = await SignInAsync(Role.Names.Organizer);

        var response = await SendAsync(HttpMethod.Post, "/api/events", token, await NewEventAsync());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventReadDto>();
        Assert.Equal($"/api/Events/{created!.PublicId}", response.Headers.Location!.AbsolutePath);

        var read = await _client.GetFromJsonAsync<EventReadDto>($"/api/events/{created.PublicId}");
        Assert.Equal("Winter Gala", read!.Name);
        Assert.Equal(400, read.AvailableTickets);
    }

    [Fact]
    public async Task Create_AsUser_Returns403()
    {
        var token = await SignInAsync(Role.Names.User);

        var response = await SendAsync(HttpMethod.Post, "/api/events", token, await NewEventAsync());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/events", await NewEventAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidInput_Returns400WithFieldErrors()
    {
        var token = await SignInAsync(Role.Names.Organizer);
        var request = await NewEventAsync() with
        {
            Name = "",
            Date = DateTime.UtcNow.AddDays(-1),
            TicketTypes = [new("Standard", -5m, 0)]
        };

        var response = await SendAsync(HttpMethod.Post, "/api/events", token, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal("validation_failed", error!.Code);
        Assert.Contains("name", error.Errors!.Keys);
        Assert.Contains("date", error.Errors.Keys);
        Assert.Contains("ticketTypes[0].Price", error.Errors.Keys);
        Assert.Contains("ticketTypes[0].Quantity", error.Errors.Keys);
    }

    [Fact]
    public async Task UpdateAndDelete_OtherOrganizersEvent_Return403()
    {
        var owner = await SignInAsync(Role.Names.Organizer);
        var other = await SignInAsync(Role.Names.Organizer);
        var request = await NewEventAsync();
        var created = await CreateAsync(owner, request);

        var update = await SendAsync(HttpMethod.Put, $"/api/events/{created.PublicId}", other, request with { Name = "Taken over" });
        var delete = await SendAsync(HttpMethod.Delete, $"/api/events/{created.PublicId}", other);

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
        var read = await _client.GetFromJsonAsync<EventReadDto>($"/api/events/{created.PublicId}");
        Assert.Equal("Winter Gala", read!.Name);
    }

    [Fact]
    public async Task UpdateThenDelete_OwnEvent_Returns204AndTheEventIsGone()
    {
        var token = await SignInAsync(Role.Names.Organizer);
        var request = await NewEventAsync();
        var created = await CreateAsync(token, request);

        var update = await SendAsync(HttpMethod.Put, $"/api/events/{created.PublicId}", token, request with { Name = "Spring Gala" });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        var read = await _client.GetFromJsonAsync<EventReadDto>($"/api/events/{created.PublicId}");
        Assert.Equal("Spring Gala", read!.Name);

        var delete = await SendAsync(HttpMethod.Delete, $"/api/events/{created.PublicId}", token);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/events/{created.PublicId}")).StatusCode);
    }

    [Fact]
    public async Task Create_NullTicketType_Returns400()
    {
        var token = await SignInAsync(Role.Names.Organizer);
        var request = await NewEventAsync() with { TicketTypes = [null!] };

        var response = await SendAsync(HttpMethod.Post, "/api/events", token, request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Contains("ticketTypes[0]", error!.Errors!.Keys);
    }

    [Fact]
    public async Task Update_SwappingTicketTypeNames_Returns204AndKeepsEachRow()
    {
        var token = await SignInAsync(Role.Names.Organizer);
        var request = await NewEventAsync();
        var created = await CreateAsync(token, request);
        var ids = await TicketTypeIdsAsync(created.PublicId);

        var swapped = request with
        {
            TicketTypes = [new("VIP", 30m, 350, ids["Standard"]), new("Standard", 75m, 50, ids["VIP"])]
        };
        var response = await SendAsync(HttpMethod.Put, $"/api/events/{created.PublicId}", token, swapped);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var after = await TicketTypeIdsAsync(created.PublicId);
        Assert.Equal(ids["Standard"], after["VIP"]);
        Assert.Equal(ids["VIP"], after["Standard"]);
    }

    [Fact]
    public async Task Update_NewTicketTypeTakingARenamedTypesName_Returns204()
    {
        var token = await SignInAsync(Role.Names.Organizer);
        var request = await NewEventAsync();
        var created = await CreateAsync(token, request);
        var ids = await TicketTypeIdsAsync(created.PublicId);

        var renamed = request with
        {
            TicketTypes = [new("Regular", 30m, 300, ids["Standard"]), new("Standard", 20m, 50)]
        };
        var response = await SendAsync(HttpMethod.Put, $"/api/events/{created.PublicId}", token, renamed);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var after = await TicketTypeIdsAsync(created.PublicId);
        Assert.Equal(["Regular", "Standard"], after.Keys.Order());
        Assert.Equal(ids["Standard"], after["Regular"]);
    }

    private async Task<string> SignInAsync(string role)
    {
        var email = $"organizer.{Guid.NewGuid():N}@ticksi.com";
        var register = await _client.PostAsJsonAsync("/api/auth/register",
            new { FirstName = "Lejla", LastName = "Begic", Email = email, Password, Phone = "+387 61 123 456" });
        register.EnsureSuccessStatusCode();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await context.AppUsers.SingleAsync(u => u.Email == email);
            user.RoleId = await context.Roles.Where(r => r.Name == role).Select(r => r.Id).SingleAsync();
            await context.SaveChangesAsync();
        }

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password });
        login.EnsureSuccessStatusCode();
        return (await login.Content.ReadFromJsonAsync<AuthResponseDto>())!.AccessToken;
    }

    private async Task<Dictionary<string, Guid>> TicketTypeIdsAsync(Guid eventPublicId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.TicketTypes
            .Where(t => t.Event!.PublicId == eventPublicId)
            .ToDictionaryAsync(t => t.Name, t => t.PublicId);
    }

    private async Task<EventRequest> NewEventAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new EventCategory { Name = "Music" };
        var venue = new Location { Name = "Zetra", City = "Sarajevo", Address = "Alipašina bb", Capacity = 500 };
        var company = new OrganizerCompany { Name = "Ticksi Events", Email = "events@ticksi.com" };
        context.AddRange(category, venue, company);
        await context.SaveChangesAsync();

        return new EventRequest(
            "Winter Gala", "An evening of music.", DateTime.UtcNow.AddMonths(2), "gala@ticksi.com",
            category.PublicId, ConcertTypeId, venue.PublicId, company.PublicId,
            [new("Standard", 30m, 350), new("VIP", 75m, 50)]);
    }

    private async Task<EventReadDto> CreateAsync(string token, EventRequest request)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/events", token, request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EventReadDto>())!;
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return _client.SendAsync(request);
    }

    private sealed record EventRequest(
        string Name,
        string Description,
        DateTime Date,
        string Contact,
        Guid CategoryId,
        Guid EventTypeId,
        Guid LocationId,
        Guid OrganizerCompanyId,
        List<TicketTypeRequest> TicketTypes);

    private sealed record TicketTypeRequest(string Name, decimal Price, int Quantity, Guid? PublicId = null);

    private sealed record ErrorBody(string Code, string Message, string TraceId, Dictionary<string, string[]>? Errors);
}
