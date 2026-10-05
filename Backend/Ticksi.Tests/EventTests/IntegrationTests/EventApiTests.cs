using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.DTOs;
using Ticksi.Domain.Enums;
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

    [Fact]
    public async Task GetManaged_AsOrganizer_ListsOwnEventsSortedByPaidTickets()
    {
        var token = await SignInAsync(Role.Names.Organizer);
        var other = await SignInAsync(Role.Names.Organizer);
        var tag = Guid.NewGuid().ToString("N")[..8];
        var request = await NewEventAsync();
        var quiet = await CreateAsync(token, request with { Name = $"Quiet {tag}" });
        var popular = await CreateAsync(token, request with { Name = $"Popular {tag}" });
        await CreateAsync(other, request with { Name = $"Foreign {tag}" });
        await AddOrderAsync(popular.PublicId, 5, OrderStatus.Paid);
        await AddOrderAsync(quiet.PublicId, 9, OrderStatus.Pending);

        var response = await SendAsync(HttpMethod.Get, $"/api/events/managed?name={tag}&sortBy=sold&sortDescending=true", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PageBody<ManagedEventBody>>();
        Assert.Equal(2, page!.TotalCount);
        Assert.Equal([($"Popular {tag}", 5), ($"Quiet {tag}", 0)], page.Items.Select(e => (e.Name, e.TicketsSold)));
        Assert.All(page.Items, e => Assert.Equal(("Zetra", 400), (e.VenueName, e.TicketsTotal)));
    }

    [Fact]
    public async Task GetManaged_AsUserOrWithoutToken_IsRefused()
    {
        var token = await SignInAsync(Role.Names.User);

        var asUser = await SendAsync(HttpMethod.Get, "/api/events/managed", token);
        var anonymous = await _client.GetAsync("/api/events/managed");

        Assert.Equal(HttpStatusCode.Forbidden, asUser.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task GetManaged_InvalidQuery_Returns400WithFieldErrors()
    {
        var token = await SignInAsync(Role.Names.Organizer);

        var response = await SendAsync(HttpMethod.Get, "/api/events/managed?sortBy=price&period=soon&pageSize=500", token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal(["pageSize", "period", "sortBy"], error!.Errors!.Keys.Order());
    }

    [Fact]
    public async Task GetManaged_LatestPossibleEndDate_Returns200()
    {
        var token = await SignInAsync(Role.Names.Organizer);
        var created = await CreateAsync(token, await NewEventAsync());

        var response = await SendAsync(HttpMethod.Get, "/api/events/managed?dateFrom=0001-01-01&dateTo=9999-12-31", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.Content.ReadFromJsonAsync<PageBody<ManagedEventBody>>();
        Assert.Equal(created.Name, Assert.Single(page!.Items).Name);
    }

    [Fact]
    public async Task GetForEdit_OwnEventOnly()
    {
        var token = await SignInAsync(Role.Names.Organizer);
        var other = await SignInAsync(Role.Names.Organizer);
        var request = await NewEventAsync();
        var created = await CreateAsync(token, request);

        var own = await SendAsync(HttpMethod.Get, $"/api/events/{created.PublicId}/edit", token);
        var foreign = await SendAsync(HttpMethod.Get, $"/api/events/{created.PublicId}/edit", other);

        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
        var body = await own.Content.ReadFromJsonAsync<EventRequest>();
        Assert.Equal(request.LocationId, body!.LocationId);
        Assert.Equal(["Standard", "VIP"], body.TicketTypes.Select(t => t.Name));
    }

    [Fact]
    public async Task GetFormOptions_AsOrganizer_ListsVenuesTypesAndCompanies()
    {
        var token = await SignInAsync(Role.Names.Organizer);
        var request = await NewEventAsync();

        var options = await (await SendAsync(HttpMethod.Get, "/api/events/form-options", token))
            .Content.ReadFromJsonAsync<FormOptionsBody>();

        Assert.Contains(options!.Venues, v => v.PublicId == request.LocationId && v.Capacity == 500);
        Assert.Contains(options.EventTypes, t => t.PublicId == ConcertTypeId);
        Assert.Contains(options.OrganizerCompanies, c => c.PublicId == request.OrganizerCompanyId);
    }

    [Fact]
    public async Task Poster_UploadReplaceAndDelete_KeepOnlyTheCurrentFileOnDisk()
    {
        var token = await SignInAsync(Role.Names.Organizer);
        var created = await CreateAsync(token, await NewEventAsync());

        var first = await UploadPosterAsync(token, created.PublicId, "poster.png", TestImages.Png);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstUrl = (await first.Content.ReadFromJsonAsync<PosterBody>())!.PosterUrl;

        var read = await _client.GetFromJsonAsync<EventReadDto>($"/api/events/{created.PublicId}");
        Assert.Equal(firstUrl, read!.PosterUrl);
        Assert.Equal(TestImages.Png, await _client.GetByteArrayAsync(firstUrl));

        var second = await UploadPosterAsync(token, created.PublicId, "poster.jpg", TestImages.Jpeg);
        var secondUrl = (await second.Content.ReadFromJsonAsync<PosterBody>())!.PosterUrl;
        Assert.False(File.Exists(PathOnDisk(firstUrl)));
        Assert.True(File.Exists(PathOnDisk(secondUrl)));

        await SendAsync(HttpMethod.Delete, $"/api/events/{created.PublicId}", token);
        Assert.False(File.Exists(PathOnDisk(secondUrl)));
    }

    [Fact]
    public async Task Poster_ParallelReplacements_LeaveOnlyTheSavedPosterOnDisk()
    {
        var token = await SignInAsync(Role.Names.Organizer);
        var created = await CreateAsync(token, await NewEventAsync());
        var first = await UploadPosterAsync(token, created.PublicId, "poster.png", TestImages.Png);
        var firstUrl = (await first.Content.ReadFromJsonAsync<PosterBody>())!.PosterUrl;
        var before = PostersOnDisk();

        var responses = await Task.WhenAll(Enumerable.Range(0, 6)
            .Select(_ => UploadPosterAsync(token, created.PublicId, "poster.png", TestImages.Png)));

        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        var saved = await _client.GetFromJsonAsync<EventReadDto>($"/api/events/{created.PublicId}");
        var expected = before.Except([PathOnDisk(firstUrl)]).Append(PathOnDisk(saved!.PosterUrl!));
        Assert.Equal(expected.Order(), PostersOnDisk().Order());
    }

    [Fact]
    public async Task Poster_OtherOrganizersEvent_Returns403()
    {
        var owner = await SignInAsync(Role.Names.Organizer);
        var other = await SignInAsync(Role.Names.Organizer);
        var created = await CreateAsync(owner, await NewEventAsync());

        var response = await UploadPosterAsync(other, created.PublicId, "poster.png", TestImages.Png);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Poster_TextFileNamedAsImage_Returns400OnTheFile()
    {
        var token = await SignInAsync(Role.Names.Organizer);
        var created = await CreateAsync(token, await NewEventAsync());

        var response = await UploadPosterAsync(token, created.PublicId, "poster.png", TestImages.Text);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal(["file"], error!.Errors!.Keys);
    }

    [Fact]
    public async Task Poster_WithoutFile_Returns400OnTheFile()
    {
        var token = await SignInAsync(Role.Names.Organizer);
        var created = await CreateAsync(token, await NewEventAsync());

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/events/{created.PublicId}/poster")
        {
            Content = new MultipartFormDataContent { { new StringContent("Winter Gala"), "name" } }
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal(["file"], error!.Errors!.Keys);
    }

    [Fact]
    public async Task Poster_WithoutToken_Returns401()
    {
        using var content = PosterContent("poster.png", TestImages.Png);

        var response = await _client.PutAsync($"/api/events/{Guid.NewGuid()}/poster", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task AddOrderAsync(Guid eventPublicId, int quantity, OrderStatus status)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ticketType = await context.TicketTypes.FirstAsync(t => t.Event!.PublicId == eventPublicId);
        var buyer = await context.AppUsers.FirstAsync();

        context.Orders.Add(new Order
        {
            AppUserId = buyer.Id,
            Status = status,
            TotalAmount = ticketType.Price * quantity,
            Items = [new OrderItem { TicketTypeId = ticketType.Id, Quantity = quantity, UnitPrice = ticketType.Price }]
        });
        await context.SaveChangesAsync();
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

    private Task<HttpResponseMessage> UploadPosterAsync(string token, Guid eventPublicId, string fileName, byte[] content)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/events/{eventPublicId}/poster")
        {
            Content = PosterContent(fileName, content)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }

    private static MultipartFormDataContent PosterContent(string fileName, byte[] content) =>
        new() { { new ByteArrayContent(content), "file", fileName } };

    private string PathOnDisk(string url) => Path.GetFullPath(Path.Combine(factory.WebRoot, url.TrimStart('/')));

    private string[] PostersOnDisk() =>
        Directory.GetFiles(Path.Combine(factory.WebRoot, "images", "events")).Select(Path.GetFullPath).ToArray();

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

    private sealed record PageBody<T>(List<T> Items, int TotalCount);

    private sealed record ManagedEventBody(string Name, string VenueName, int TicketsSold, int TicketsTotal);

    private sealed record OptionBody(Guid PublicId, string Name, int Capacity);

    private sealed record FormOptionsBody(List<OptionBody> Venues, List<OptionBody> EventTypes, List<OptionBody> OrganizerCompanies);

    private sealed record PosterBody(string PosterUrl);

    private sealed record ErrorBody(string Code, string Message, string TraceId, Dictionary<string, string[]>? Errors);
}
