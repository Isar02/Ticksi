using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Users;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.UserTests.IntegrationTests;

public class UserApiTests(TicksiApiFactory factory) : IClassFixture<TicksiApiFactory>
{
    private const string Password = "Secret123";
    private static readonly Guid UserRoleId = Guid.Parse("8d0c3f52-6b1e-4f4a-9c35-1f2e7a9b0c02");
    private static readonly Guid OrganizerRoleId = Guid.Parse("8d0c3f52-6b1e-4f4a-9c35-1f2e7a9b0c03");

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Create_AsAdmin_Returns201AndTheNewAccountCanSignIn()
    {
        var admin = await SignInAsync(Role.Names.Admin);
        var request = NewUser();

        var response = await SendAsync(HttpMethod.Post, "/api/users", admin.AccessToken, request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal(Role.Names.Organizer, created!.RoleName);
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { request.Email, Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Create_AsOrganizerOrAnonymous_Returns403And401()
    {
        var organizer = await SignInAsync(Role.Names.Organizer);

        var forbidden = await SendAsync(HttpMethod.Post, "/api/users", organizer.AccessToken, NewUser());
        var anonymous = await _client.PostAsJsonAsync("/api/users", NewUser());

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidOrTakenEmail_Returns400WithFieldErrors()
    {
        var admin = await SignInAsync(Role.Names.Admin);

        var invalid = await SendAsync(HttpMethod.Post, "/api/users", admin.AccessToken,
            NewUser() with { FirstName = "", Phone = "x", Password = "1" });
        var taken = await SendAsync(HttpMethod.Post, "/api/users", admin.AccessToken, NewUser() with { Email = admin.Email });

        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        var invalidError = await invalid.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Contains("firstName", invalidError!.Errors!.Keys);
        Assert.Contains("phone", invalidError.Errors.Keys);
        Assert.Contains("password", invalidError.Errors.Keys);

        Assert.Equal(HttpStatusCode.BadRequest, taken.StatusCode);
        var takenError = await taken.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal(["An account with this email already exists."], takenError!.Errors!["email"]);
    }

    [Fact]
    public async Task Deactivate_EndsTheAccountsSessionAndBlocksSignIn()
    {
        var admin = await SignInAsync(Role.Names.Admin);
        var user = await SignInAsync(Role.Names.User);

        var deactivate = await SendAsync(HttpMethod.Put, $"/api/users/{user.PublicId}/active", admin.AccessToken, new { IsActive = false });

        Assert.Equal(HttpStatusCode.NoContent, deactivate.StatusCode);
        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new { user.RefreshToken });
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { user.Email, Password });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        var activate = await SendAsync(HttpMethod.Put, $"/api/users/{user.PublicId}/active", admin.AccessToken, new { IsActive = true });
        Assert.Equal(HttpStatusCode.NoContent, activate.StatusCode);
        var loginAgain = await _client.PostAsJsonAsync("/api/auth/login", new { user.Email, Password });
        Assert.Equal(HttpStatusCode.OK, loginAgain.StatusCode);
    }

    [Fact]
    public async Task Update_ChangesTheRoleAndTheNewRoleIsInTheNextToken()
    {
        var admin = await SignInAsync(Role.Names.Admin);
        var user = await SignInAsync(Role.Names.User);
        var request = NewUser() with { Email = user.Email, RoleId = OrganizerRoleId };

        var update = await SendAsync(HttpMethod.Put, $"/api/users/{user.PublicId}", admin.AccessToken, request);

        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);
        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new { user.RefreshToken });
        var session = await refresh.Content.ReadFromJsonAsync<AuthResponseDto>();
        var forms = await SendAsync(HttpMethod.Get, "/api/events/form-options", session!.AccessToken);
        Assert.Equal(HttpStatusCode.OK, forms.StatusCode);
    }

    [Fact]
    public async Task OwnAccount_CannotBeDeactivatedDemotedOrDeleted()
    {
        var admin = await SignInAsync(Role.Names.Admin);

        var deactivate = await SendAsync(HttpMethod.Put, $"/api/users/{admin.PublicId}/active", admin.AccessToken, new { IsActive = false });
        var demote = await SendAsync(HttpMethod.Put, $"/api/users/{admin.PublicId}", admin.AccessToken,
            NewUser() with { Email = admin.Email, RoleId = UserRoleId });
        var delete = await SendAsync(HttpMethod.Delete, $"/api/users/{admin.PublicId}", admin.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, deactivate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, demote.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }

    [Fact]
    public async Task Delete_AccountWithoutHistory_Returns204AndUnknownReturns404()
    {
        var admin = await SignInAsync(Role.Names.Admin);
        var user = await SignInAsync(Role.Names.User);

        var delete = await SendAsync(HttpMethod.Delete, $"/api/users/{user.PublicId}", admin.AccessToken);
        var again = await SendAsync(HttpMethod.Delete, $"/api/users/{user.PublicId}", admin.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        var login = await _client.PostAsJsonAsync("/api/auth/login", new { user.Email, Password });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    private static UserRequest NewUser() =>
        new("Amar", "Hadzic", $"amar.{Guid.NewGuid():N}@ticksi.com", "+387 62 555 444", OrganizerRoleId, true, Password);

    private async Task<AuthResponseDto> SignInAsync(string role)
    {
        var email = $"{role.ToLowerInvariant()}.{Guid.NewGuid():N}@ticksi.com";
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
        return (await login.Content.ReadFromJsonAsync<AuthResponseDto>())!;
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
            request.Content = JsonContent.Create(body);
        return _client.SendAsync(request);
    }

    private sealed record UserRequest(
        string FirstName,
        string LastName,
        string Email,
        string Phone,
        Guid RoleId,
        bool IsActive,
        string Password);

    private sealed record ErrorBody(string Code, string Message, string TraceId, Dictionary<string, string[]>? Errors);
}
