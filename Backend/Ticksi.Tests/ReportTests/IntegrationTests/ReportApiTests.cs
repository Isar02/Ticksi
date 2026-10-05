using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.DTOs;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.ReportTests.IntegrationTests;

public class ReportApiTests(TicksiApiFactory factory) : IClassFixture<TicksiApiFactory>
{
    private const string Password = "Secret123";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task EventsByCategory_AsOrganizerWithRange_ReturnsAPdf()
    {
        var token = await SignInAsync(Role.Names.Organizer);
        var category = await AddCategoryAsync();

        var response = await GetAsync($"/api/reports/events-by-category/{category}?dateFrom=2026-10-01&dateTo=2026-10-31", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        var pdf = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf, 0, 4));
    }

    [Fact]
    public async Task EventsByCategory_EndBeforeStart_Returns400OnTheEndDate()
    {
        var token = await SignInAsync(Role.Names.Admin);
        var category = await AddCategoryAsync();

        var response = await GetAsync($"/api/reports/events-by-category/{category}?dateFrom=2026-10-31&dateTo=2026-10-01", token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal(["dateTo"], error!.Errors!.Keys);
    }

    [Fact]
    public async Task EventsByCategory_AsUser_Returns403()
    {
        var token = await SignInAsync(Role.Names.User);
        var category = await AddCategoryAsync();

        var response = await GetAsync($"/api/reports/events-by-category/{category}", token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<Guid> AddCategoryAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = new EventCategory { Name = "Music" };
        context.Events.Add(new Event
        {
            Name = "Jazz Night",
            Description = "Open air.",
            Date = new DateTime(2026, 10, 8, 20, 0, 0),
            Contact = "events@ticksi.com",
            AppUser = await context.AppUsers.FirstAsync(),
            EventCategory = category,
            Location = new Location { Name = "Zetra", City = "Sarajevo", Address = "Alipašina bb", Capacity = 500 },
            OrganizerCompany = new OrganizerCompany { Name = "Ticksi Events", Email = "events@ticksi.com" },
            EventType = await context.EventTypes.FirstAsync(),
            TicketTypes = [new() { Name = "Standard", Price = 25m, Quantity = 100 }]
        });
        await context.SaveChangesAsync();
        return category.PublicId;
    }

    private async Task<string> SignInAsync(string role)
    {
        var email = $"reports.{Guid.NewGuid():N}@ticksi.com";
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

    private Task<HttpResponseMessage> GetAsync(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(request);
    }

    private sealed record ErrorBody(string Code, string Message, string TraceId, Dictionary<string, string[]>? Errors);
}
