using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Events.Queries.GetEventTicketTypes;
using Ticksi.Application.Features.Orders;
using Ticksi.Domain.Enums;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.OrderTests.IntegrationTests;

public class OrderApiTests(TicksiApiFactory factory) : IClassFixture<TicksiApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetTicketTypes_Anonymously_ListsPricesAndTicketsLeft()
    {
        var eventId = await AddEventAsync(standard: 100, reserved: 40);

        var ticketTypes = await _client.GetFromJsonAsync<List<EventTicketTypeDto>>($"/api/events/{eventId}/ticket-types");

        Assert.Equal([("Standard", 30m, 60), ("VIP", 80m, 20)], ticketTypes!.Select(t => (t.Name, t.Price, t.Available)));
    }

    [Fact]
    public async Task GetTicketTypes_UnknownEvent_Returns404()
    {
        var response = await _client.GetAsync($"/api/events/{Guid.NewGuid()}/ticket-types");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_SignedIn_Returns201AndTheBuyerCanReadThePendingOrder()
    {
        var token = await RegisterAsync();
        var eventId = await AddEventAsync();
        var standard = await TicketTypeIdAsync(eventId, "Standard");

        var response = await SendAsync(HttpMethod.Post, "/api/orders", token, Order((standard, 3)));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<OrderDto>())!;
        Assert.Equal($"/api/Orders/{created.PublicId}", response.Headers.Location!.AbsolutePath);
        Assert.Contains("\"status\":\"Pending\"", await response.Content.ReadAsStringAsync());

        var read = await SendAsync(HttpMethod.Get, $"/api/orders/{created.PublicId}", token);
        var order = (await read.Content.ReadFromJsonAsync<OrderDto>())!;
        Assert.Equal((OrderStatus.Pending, 90m), (order.Status, order.TotalAmount));
        Assert.Equal(("Concert", "Standard", 3, 30m), Line(Assert.Single(order.Items)));

        var left = await _client.GetFromJsonAsync<List<EventTicketTypeDto>>($"/api/events/{eventId}/ticket-types");
        Assert.Equal(97, left!.Single(t => t.Name == "Standard").Available);
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var eventId = await AddEventAsync();

        var response = await _client.PostAsJsonAsync("/api/orders", Order((await TicketTypeIdAsync(eventId, "VIP"), 1)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_QuantityAboveTheLimit_Returns400WithTheField()
    {
        var token = await RegisterAsync();
        var eventId = await AddEventAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/orders", token,
            Order((await TicketTypeIdAsync(eventId, "Standard"), OrderItem.Constraints.MaxQuantity + 1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal("validation_failed", error!.Code);
        Assert.Equal(["items[0].Quantity"], error.Errors!.Keys);
    }

    [Fact]
    public async Task Create_MoreThanIsLeft_Returns409AndReservesNothing()
    {
        var token = await RegisterAsync();
        var eventId = await AddEventAsync(standard: 10, reserved: 8);
        var standard = await TicketTypeIdAsync(eventId, "Standard");

        var response = await SendAsync(HttpMethod.Post, "/api/orders", token,
            Order((await TicketTypeIdAsync(eventId, "VIP"), 1), (standard, 3)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal("Only 2 \"Standard\" tickets are left for \"Concert\".", error!.Message);
        var left = await _client.GetFromJsonAsync<List<EventTicketTypeDto>>($"/api/events/{eventId}/ticket-types");
        Assert.Equal([2, 20], left!.Select(t => t.Available));
    }

    [Fact]
    public async Task Create_ManyBuyersAtOnceForTheLastTickets_NeverOversells()
    {
        var eventId = await AddEventAsync(vip: 5);
        var standard = await TicketTypeIdAsync(eventId, "Standard");
        var vip = await TicketTypeIdAsync(eventId, "VIP");
        var tokens = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => RegisterAsync()));

        var responses = await Task.WhenAll(tokens.Select(token =>
            SendAsync(HttpMethod.Post, "/api/orders", token, Order((standard, 1), (vip, 2)))));

        var created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }));
        Assert.InRange(created, 1, 2);

        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reserved = await context.TicketTypes
            .Where(t => t.Event!.PublicId == eventId)
            .OrderBy(t => t.Name)
            .Select(t => new { t.QuantityReserved, Ordered = t.OrderItems.Sum(i => i.Quantity) })
            .ToListAsync();
        Assert.Equal([new { QuantityReserved = created, Ordered = created }, new { QuantityReserved = created * 2, Ordered = created * 2 }], reserved);
    }

    [Fact]
    public async Task GetById_AnotherUsersOrder_Returns404()
    {
        var buyer = await RegisterAsync();
        var other = await RegisterAsync();
        var eventId = await AddEventAsync();
        var response = await SendAsync(HttpMethod.Post, "/api/orders", buyer, Order((await TicketTypeIdAsync(eventId, "VIP"), 1)));
        var created = (await response.Content.ReadFromJsonAsync<OrderDto>())!;

        var read = await SendAsync(HttpMethod.Get, $"/api/orders/{created.PublicId}", other);

        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
    }

    private static object Order(params (Guid TicketTypeId, int Quantity)[] items) =>
        new { Items = items.Select(i => new { i.TicketTypeId, i.Quantity }) };

    private static (string, string, int, decimal) Line(OrderItemDto item) =>
        (item.EventName, item.TicketTypeName, item.Quantity, item.UnitPrice);

    private async Task<string> RegisterAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            FirstName = "Lejla",
            LastName = "Begic",
            Email = $"buyer.{Guid.NewGuid():N}@ticksi.com",
            Password = "Secret123",
            Phone = "+387 61 123 456"
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!.AccessToken;
    }

    private async Task<Guid> AddEventAsync(int standard = 100, int reserved = 0, int vip = 20)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var item = new Event
        {
            Name = "Concert",
            Description = "Open air.",
            Date = DateTime.UtcNow.AddMonths(2),
            Contact = "events@ticksi.com",
            AppUser = new AppUser
            {
                FirstName = "Amra",
                LastName = "Hodzic",
                Email = $"organizer.{Guid.NewGuid():N}@ticksi.com",
                Phone = "+387 61 123 456",
                PasswordHash = "hash",
                Role = await context.Roles.SingleAsync(r => r.Name == Role.Names.Organizer)
            },
            EventCategory = new EventCategory { Name = "Music" },
            Location = new Location { Name = "Zetra", City = "Sarajevo", Address = "Alipašina bb", Capacity = 500 },
            OrganizerCompany = new OrganizerCompany { Name = "Ticksi Events", Email = "events@ticksi.com" },
            EventType = await context.EventTypes.FirstAsync(),
            TicketTypes =
            [
                new() { Name = "Standard", Price = 30m, Quantity = standard, QuantityReserved = reserved },
                new() { Name = "VIP", Price = 80m, Quantity = vip }
            ]
        };

        context.Events.Add(item);
        await context.SaveChangesAsync();
        return item.PublicId;
    }

    private async Task<Guid> TicketTypeIdAsync(Guid eventId, string name)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.TicketTypes
            .Where(t => t.Event!.PublicId == eventId && t.Name == name)
            .Select(t => t.PublicId)
            .SingleAsync();
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }

    private sealed record ErrorBody(string Code, string Message, string TraceId, Dictionary<string, string[]>? Errors);
}
