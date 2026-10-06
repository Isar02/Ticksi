using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Orders;
using Ticksi.Application.Features.Payments;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.PaymentTests.IntegrationTests;

public class PaymentApiTests : IClassFixture<TicksiApiFactory>
{
    private readonly TicksiApiFactory _factory;
    private readonly FakePaymentGateway _gateway = new();
    private readonly HttpClient _client;

    public PaymentApiTests(TicksiApiFactory factory)
    {
        _factory = factory;
        _client = factory
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services => services.AddSingleton<IPaymentGateway>(_gateway)))
            .CreateClient();
    }

    [Fact]
    public async Task Pay_StartThenWebhook_MarksThePaidOrderAndIssuesTickets()
    {
        var token = await RegisterAsync();
        var order = await PlaceOrderAsync(token);

        var start = await SendAsync(HttpMethod.Post, $"/api/orders/{order.PublicId}/payment", token);

        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        var session = (await start.Content.ReadFromJsonAsync<PaymentSessionDto>())!;
        Assert.Equal(("pk_test_fake", 70m, "BAM", false), (session.PublishableKey, session.Amount, session.Currency, session.Paid));
        Assert.NotEmpty(session.ClientSecret!);

        Assert.Equal(order.PublicId, Assert.Single(_gateway.Created).OrderId);
        var reported = _gateway.Report(await ReferenceOfAsync(order.PublicId), GatewayPaymentState.Succeeded);
        _gateway.Events["paid"] = new GatewayEvent(GatewayEventKind.PaymentSucceeded, reported);

        var webhook = await PostWebhookAsync("paid", FakePaymentGateway.ValidSignature);
        var read = await (await SendAsync(HttpMethod.Get, $"/api/orders/{order.PublicId}", token)).Content.ReadFromJsonAsync<OrderDto>();

        Assert.Equal(HttpStatusCode.OK, webhook.StatusCode);
        Assert.Equal(OrderStatus.Paid, read!.Status);
        Assert.Equal(2, await TicketCountAsync(order.PublicId));
    }

    [Fact]
    public async Task Confirm_AfterTheCardWasCharged_ReturnsThePaidOrder()
    {
        var token = await RegisterAsync();
        var order = await PlaceOrderAsync(token);
        await SendAsync(HttpMethod.Post, $"/api/orders/{order.PublicId}/payment", token);
        _gateway.Report(await ReferenceOfAsync(order.PublicId), GatewayPaymentState.Succeeded);

        var response = await SendAsync(HttpMethod.Post, $"/api/orders/{order.PublicId}/payment/confirm", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"Paid\"", await response.Content.ReadAsStringAsync());
        Assert.Equal(2, await TicketCountAsync(order.PublicId));
    }

    [Fact]
    public async Task Webhook_WithAForgedSignature_Returns400()
    {
        var response = await PostWebhookAsync("{}", "forged");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Webhook_WithoutASignature_Returns400()
    {
        var response = await _client.PostAsync("/api/payments/webhook", new StringContent("{}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Start_ForAnotherUsersOrder_Returns404AndWithoutToken401()
    {
        var order = await PlaceOrderAsync(await RegisterAsync());

        var foreign = await SendAsync(HttpMethod.Post, $"/api/orders/{order.PublicId}/payment", await RegisterAsync());
        var anonymous = await _client.PostAsync($"/api/orders/{order.PublicId}/payment", null);

        Assert.Equal(HttpStatusCode.NotFound, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Start_WhenTheCardServiceIsDown_Returns502()
    {
        var token = await RegisterAsync();
        var order = await PlaceOrderAsync(token);
        _gateway.Unavailable = true;

        var response = await SendAsync(HttpMethod.Post, $"/api/orders/{order.PublicId}/payment", token);

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Contains("payment_unavailable", await response.Content.ReadAsStringAsync());
    }

    private Task<HttpResponseMessage> PostWebhookAsync(string payload, string signature)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/payments/webhook") { Content = new StringContent(payload) };
        request.Headers.Add("Stripe-Signature", signature);
        return _client.SendAsync(request);
    }

    private async Task<OrderDto> PlaceOrderAsync(string token)
    {
        var eventId = await AddEventAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var standard = await context.TicketTypes.Where(t => t.Event!.PublicId == eventId).Select(t => t.PublicId).SingleAsync();

        var response = await SendAsync(HttpMethod.Post, "/api/orders", token,
            new { Items = new[] { new { TicketTypeId = standard, Quantity = 2 } } });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    private async Task<Guid> AddEventAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var item = new Event
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
            TicketTypes = [new() { Name = "Standard", Price = 35m, Quantity = 100 }]
        };

        context.Events.Add(item);
        await context.SaveChangesAsync();
        return item.PublicId;
    }

    private async Task<string> ReferenceOfAsync(Guid orderId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.Payments.Where(p => p.Order!.PublicId == orderId).Select(p => p.ProviderReference).SingleAsync();
    }

    private async Task<int> TicketCountAsync(Guid orderId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.Tickets.CountAsync(t => t.OrderItem!.Order!.PublicId == orderId);
    }

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

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = body is null ? null : JsonContent.Create(body) };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }
}
