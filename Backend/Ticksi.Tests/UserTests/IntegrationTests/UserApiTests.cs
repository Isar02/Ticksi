using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.Common;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Users;
using Ticksi.Application.Features.Users.Queries.GetRoles;
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
        var loaded = await SendAsync(HttpMethod.Get, response.Headers.Location!.PathAndQuery, admin.AccessToken);
        Assert.Equal(request.Email, (await loaded.Content.ReadFromJsonAsync<UserDto>())!.Email);
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

    [Fact]
    public async Task List_AsAdmin_AppliesTheFiltersSortAndPaging()
    {
        var admin = await SignInAsync(Role.Names.Admin);
        var marker = Guid.NewGuid().ToString("N");
        await CreateAsync(admin, NewUser() with { Email = $"a.{marker}@ticksi.com" });
        await CreateAsync(admin, NewUser() with { Email = $"b.{marker}@ticksi.com", RoleId = UserRoleId, IsActive = false });
        await CreateAsync(admin, NewUser() with { Email = $"c.{marker}@ticksi.com", RoleId = UserRoleId });

        var all = await ListAsync(admin, $"search={marker}&sortBy=email&sortDescending=true&pageSize=2");
        var organizers = await ListAsync(admin, $"search={marker}&roleId={OrganizerRoleId}");
        var inactive = await ListAsync(admin, $"search={marker}&isActive=false");
        var yesterday = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var tomorrow = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");
        var registeredRecently = await ListAsync(admin, $"search={marker}&registeredFrom={yesterday}&registeredTo={tomorrow}");
        var registeredLater = await ListAsync(admin, $"search={marker}&registeredFrom={tomorrow}");

        Assert.Equal([$"c.{marker}@ticksi.com", $"b.{marker}@ticksi.com"], all.Items.Select(u => u.Email));
        Assert.Equal((3, 2), (all.TotalCount, all.TotalPages));
        Assert.Equal([$"a.{marker}@ticksi.com"], organizers.Items.Select(u => u.Email));
        Assert.Equal([$"b.{marker}@ticksi.com"], inactive.Items.Select(u => u.Email));
        Assert.Equal((3, 0), (registeredRecently.TotalCount, registeredLater.TotalCount));
    }

    [Fact]
    public async Task List_InvalidQuery_Returns400WithFieldErrors()
    {
        var admin = await SignInAsync(Role.Names.Admin);

        var response = await SendAsync(HttpMethod.Get,
            "/api/users?sortBy=phone&pageSize=500&registeredFrom=2026-09-02&registeredTo=2026-09-01", admin.AccessToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal(["pageSize", "registeredTo", "sortBy"], error!.Errors!.Keys.Order());
    }

    [Theory]
    [InlineData("/api/users")]
    [InlineData("/api/users/roles")]
    [InlineData("/api/users/8d0c3f52-6b1e-4f4a-9c35-1f2e7a9b0c99")]
    public async Task Reads_AsOrganizerOrAnonymous_Return403And401(string url)
    {
        var organizer = await SignInAsync(Role.Names.Organizer);

        var forbidden = await SendAsync(HttpMethod.Get, url, organizer.AccessToken);
        var anonymous = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
    }

    [Fact]
    public async Task GetById_ReturnsTheAccountOr404_AndRolesListsEveryRole()
    {
        var admin = await SignInAsync(Role.Names.Admin);
        var user = await SignInAsync(Role.Names.User);

        var found = await SendAsync(HttpMethod.Get, $"/api/users/{user.PublicId}", admin.AccessToken);
        var missing = await SendAsync(HttpMethod.Get, $"/api/users/{Guid.NewGuid()}", admin.AccessToken);
        var roles = await SendAsync(HttpMethod.Get, "/api/users/roles", admin.AccessToken);

        var dto = await found.Content.ReadFromJsonAsync<UserDto>();
        Assert.Equal((user.Email, UserRoleId, true), (dto!.Email, dto.RoleId, dto.IsActive));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        var roleNames = (await roles.Content.ReadFromJsonAsync<List<RoleDto>>())!.Select(r => r.Name);
        Assert.Equal([Role.Names.Admin, Role.Names.Organizer, Role.Names.User], roleNames);
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

    private async Task CreateAsync(AuthResponseDto admin, UserRequest request)
    {
        var response = await SendAsync(HttpMethod.Post, "/api/users", admin.AccessToken, request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<PagedResult<UserDto>> ListAsync(AuthResponseDto admin, string query)
    {
        var response = await SendAsync(HttpMethod.Get, $"/api/users?{query}", admin.AccessToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>())!;
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
