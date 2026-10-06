using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Tickets;
using Ticksi.Domain.Enums;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.TicketTests.IntegrationTests;

public class TicketApiTests(TicksiApiFactory factory) : IClassFixture<TicksiApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetMine_ListsTheBuyersTicketsWithTheirEvent()
    {
        var (email, token) = await RegisterAsync();
        var codes = await AddPaidTicketsAsync(email, 2);

        var response = await SendAsync("/api/tickets", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"Valid\"", await response.Content.ReadAsStringAsync());
        var tickets = (await response.Content.ReadFromJsonAsync<List<TicketDto>>())!;
        Assert.Equal(codes, tickets.Select(t => t.Code));
        Assert.All(tickets, t => Assert.Equal(("Concert", "Standard", "Zetra"), (t.EventName, t.TicketTypeName, t.VenueName)));
    }

    [Fact]
    public async Task GetQrCode_ForOwnTicket_ReturnsAPngImage()
    {
        var (email, token) = await RegisterAsync();
        await AddPaidTicketsAsync(email, 1);
        var ticket = Assert.Single((await (await SendAsync("/api/tickets", token)).Content.ReadFromJsonAsync<List<TicketDto>>())!);

        var response = await SendAsync($"/api/tickets/{ticket.PublicId}/qr", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], (await response.Content.ReadAsByteArrayAsync())[..4]);
    }

    [Fact]
    public async Task GetQrCode_ForAnotherUsersTicket_Returns404AndWithoutToken401()
    {
        var (email, token) = await RegisterAsync();
        await AddPaidTicketsAsync(email, 1);
        var ticket = Assert.Single((await (await SendAsync("/api/tickets", token)).Content.ReadFromJsonAsync<List<TicketDto>>())!);

        var foreign = await SendAsync($"/api/tickets/{ticket.PublicId}/qr", (await RegisterAsync()).Token);
        var anonymous = await _client.GetAsync($"/api/tickets/{ticket.PublicId}/qr");
        var anonymousList = await _client.GetAsync("/api/tickets");

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousList.StatusCode);
    }

    private async Task<List<string>> AddPaidTicketsAsync(string buyerEmail, int count)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var buyer = await context.AppUsers.SingleAsync(u => u.Email == buyerEmail);
        var standard = new TicketType { Name = "Standard", Price = 35m, Quantity = 100, QuantityReserved = count };
        context.Events.Add(new Event
        {
            Name = "Concert",
            Description = "Open air.",
            Date = DateTime.UtcNow.AddMonths(2),
            Contact = "events@ticksi.com",
            AppUser = new AppUser
            {
                FirstName = "Amra", LastName = "Hodzic", Email = $"organizer.{Guid.NewGuid():N}@ticksi.com",
                Phone = "+387 61 123 456", PasswordHash = "hash",
                Role = await context.Roles.SingleAsync(r => r.Name == Role.Names.Organizer)
            },
            EventCategory = new EventCategory { Name = "Music" },
            Location = new Location { Name = "Zetra", City = "Sarajevo", Address = "Alipašina bb", Capacity = 500 },
            OrganizerCompany = new OrganizerCompany { Name = "Ticksi Events", Email = "events@ticksi.com" },
            EventType = await context.EventTypes.FirstAsync(),
            TicketTypes = [standard]
        });

        var codes = Enumerable.Range(1, count).Select(_ => Guid.NewGuid().ToString("N")[..12].ToUpperInvariant()).ToList();
        context.Orders.Add(new Order
        {
            AppUserId = buyer.Id,
            Status = OrderStatus.Paid,
            TotalAmount = 35m * count,
            Items =
            [
                new OrderItem
                {
                    TicketType = standard, Quantity = count, UnitPrice = 35m,
                    Tickets = codes.Select(code => new Ticket { Code = code, IssuedAtUtc = DateTime.UtcNow }).ToList()
                }
            ]
        });

        await context.SaveChangesAsync();
        return codes;
    }

    private async Task<(string Email, string Token)> RegisterAsync()
    {
        var email = $"buyer.{Guid.NewGuid():N}@ticksi.com";
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            FirstName = "Lejla",
            LastName = "Begic",
            Email = email,
            Password = "Secret123",
            Phone = "+387 61 123 456"
        });
        response.EnsureSuccessStatusCode();
        return (email, (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!.AccessToken);
    }

    private Task<HttpResponseMessage> SendAsync(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }
}
