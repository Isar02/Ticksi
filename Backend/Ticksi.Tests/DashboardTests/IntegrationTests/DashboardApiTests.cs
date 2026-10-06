using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Dashboard;
using Ticksi.Domain.Enums;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.DashboardTests.IntegrationTests;

public class DashboardApiTests(TicksiApiFactory factory) : IClassFixture<TicksiApiFactory>
{
    private const string Password = "Secret123";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_WithoutSession_Returns401()
    {
        var response = await _client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_AsBuyer_ReturnsThePersonalOverviewWithoutSales()
    {
        var email = NewEmail();
        var token = await SignInAsync(Role.Names.User, email);
        await AddPaidOrderAsync(await AddEventAsync(organizerEmail: null), email, 2);

        var dashboard = await GetAsync(token);

        Assert.Equal(2, dashboard.UpcomingTickets);
        Assert.Equal(("Jazz Night", 2), (dashboard.NextEvent!.Name, dashboard.NextEvent.Tickets));
        Assert.Equal(OrderStatus.Paid, Assert.Single(dashboard.RecentOrders).Status);
        Assert.Null(dashboard.Sales);
    }

    [Fact]
    public async Task Get_AsOrganizer_ReturnsSalesOfOwnEvents()
    {
        var email = NewEmail();
        var token = await SignInAsync(Role.Names.Organizer, email);
        var buyerEmail = NewEmail();
        await SignInAsync(Role.Names.User, buyerEmail);
        await AddPaidOrderAsync(await AddEventAsync(email), buyerEmail, 3);

        var sales = (await GetAsync(token)).Sales!;

        Assert.Equal((1, 3, 75m), (sales.UpcomingEvents, sales.TicketsSold, sales.Revenue));
        Assert.Equal(("Jazz Night", 3), (Assert.Single(sales.TopEvents).Name, sales.TopEvents[0].TicketsSold));
        Assert.Null(sales.ActiveUsers);
    }

    private async Task<DashboardDto> GetAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DashboardDto>())!;
    }

    private async Task<Event> AddEventAsync(string? organizerEmail)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var item = new Event
        {
            Name = "Jazz Night",
            Description = "Open air.",
            Date = DateTime.UtcNow.AddMonths(2),
            Contact = "events@ticksi.com",
            AppUser = organizerEmail is null
                ? await context.AppUsers.FirstAsync()
                : await context.AppUsers.SingleAsync(u => u.Email == organizerEmail),
            EventCategory = new EventCategory { Name = "Music" },
            Location = new Location { Name = "Zetra", City = "Sarajevo", Address = "Alipašina bb", Capacity = 500 },
            OrganizerCompany = new OrganizerCompany { Name = "Ticksi Events", Email = "events@ticksi.com" },
            EventType = await context.EventTypes.FirstAsync(),
            TicketTypes = [new() { Name = "Standard", Price = 25m, Quantity = 100 }]
        };
        context.Events.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    private async Task AddPaidOrderAsync(Event item, string buyerEmail, int quantity)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ticketType = await context.TicketTypes.SingleAsync(t => t.EventId == item.Id);
        var now = DateTime.UtcNow;

        context.Orders.Add(new Order
        {
            AppUser = await context.AppUsers.SingleAsync(u => u.Email == buyerEmail),
            Status = OrderStatus.Paid,
            TotalAmount = ticketType.Price * quantity,
            CreatedAtUtc = now,
            PaidAtUtc = now,
            Items =
            [
                new OrderItem
                {
                    TicketTypeId = ticketType.Id,
                    Quantity = quantity,
                    UnitPrice = ticketType.Price,
                    Tickets = Enumerable.Range(0, quantity)
                        .Select(_ => new Ticket { Code = Guid.NewGuid().ToString("N")[..12], IssuedAtUtc = now })
                        .ToList()
                }
            ]
        });
        await context.SaveChangesAsync();
    }

    private static string NewEmail() => $"dashboard.{Guid.NewGuid():N}@ticksi.com";

    private async Task<string> SignInAsync(string role, string email)
    {
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
}
