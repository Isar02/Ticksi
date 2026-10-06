using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Profile;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.ProfileTests.IntegrationTests;

public class ProfileApiTests(TicksiApiFactory factory) : IClassFixture<TicksiApiFactory>
{
    private const string Password = "Secret123";
    private const string NewPassword = "Changed456";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_WithoutSession_Returns401()
    {
        var response = await _client.GetAsync("/api/profile");

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, "unauthorized");
    }

    [Fact]
    public async Task Get_SignedIn_ReturnsOwnDetails()
    {
        var email = NewEmail();
        var session = await RegisterAsync(email);

        var profile = await ReadAsync<ProfileDto>(await SendAsync(HttpMethod.Get, "/api/profile", session.AccessToken));

        Assert.Equal(("Lejla", "Begic", email, "+387 61 123 456", Role.Names.User),
            (profile.FirstName, profile.LastName, profile.Email, profile.Phone, profile.Role));
        Assert.True(DateTime.UtcNow - profile.RegistrationDate < TimeSpan.FromMinutes(5));
    }

    [Fact]
    public async Task Update_ValidDetails_SavesThemAndReturnsTheProfile()
    {
        var session = await RegisterAsync(NewEmail());

        var updated = await ReadAsync<ProfileDto>(await SendAsync(HttpMethod.Put, "/api/profile", session.AccessToken,
            new { FirstName = "Amra", LastName = "Hadzic", Phone = "061-987-654" }));

        var profile = await ReadAsync<ProfileDto>(await SendAsync(HttpMethod.Get, "/api/profile", session.AccessToken));
        Assert.Equal(("Amra", "Hadzic", "061-987-654"), (updated.FirstName, updated.LastName, updated.Phone));
        Assert.Equal(("Amra", "Hadzic", "061-987-654"), (profile.FirstName, profile.LastName, profile.Phone));
    }

    [Fact]
    public async Task Update_InvalidDetails_Returns400WithFieldErrors()
    {
        var session = await RegisterAsync(NewEmail());

        var response = await SendAsync(HttpMethod.Put, "/api/profile", session.AccessToken,
            new { FirstName = "A", LastName = "Hadzic", Phone = "123" });

        var error = await AssertErrorAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        Assert.Equal(["firstName", "phone"], error.Errors!.Keys.Order());
    }

    [Fact]
    public async Task ChangePassword_CorrectCurrentPassword_EndsOtherSessionsAndKeepsThisOne()
    {
        var email = NewEmail();
        var otherDevice = await RegisterAsync(email);
        var thisDevice = await LoginAsync(email, Password);

        var session = await ReadAsync<AuthResponseDto>(await SendAsync(HttpMethod.Put, "/api/profile/password", thisDevice.AccessToken,
            new { CurrentPassword = Password, NewPassword }));

        Assert.Equal(HttpStatusCode.OK, (await SendAsync(HttpMethod.Get, "/api/profile", session.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(session.RefreshToken)).StatusCode);
        await AssertErrorAsync(await RefreshAsync(otherDevice.RefreshToken), HttpStatusCode.Unauthorized, "unauthorized");
        await AssertErrorAsync(await RefreshAsync(thisDevice.RefreshToken), HttpStatusCode.Unauthorized, "unauthorized");
        await AssertErrorAsync(await PostLoginAsync(email, Password), HttpStatusCode.Unauthorized, "unauthorized");
        Assert.Equal(HttpStatusCode.OK, (await PostLoginAsync(email, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_Returns400OnCurrentPasswordAndKeepsTheSession()
    {
        var email = NewEmail();
        var session = await RegisterAsync(email);

        var response = await SendAsync(HttpMethod.Put, "/api/profile/password", session.AccessToken,
            new { CurrentPassword = "Wrong123", NewPassword });

        var error = await AssertErrorAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        Assert.Equal(["currentPassword"], error.Errors!.Keys);
        Assert.Equal(HttpStatusCode.OK, (await RefreshAsync(session.RefreshToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await PostLoginAsync(email, Password)).StatusCode);
    }

    private static string NewEmail() => $"profile.{Guid.NewGuid():N}@ticksi.com";

    private async Task<AuthResponseDto> RegisterAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new { FirstName = "Lejla", LastName = "Begic", Email = email, Password, Phone = "+387 61 123 456" });
        return await ReadAsync<AuthResponseDto>(response);
    }

    private async Task<AuthResponseDto> LoginAsync(string email, string password) =>
        await ReadAsync<AuthResponseDto>(await PostLoginAsync(email, password));

    private Task<HttpResponseMessage> PostLoginAsync(string email, string password) =>
        _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        _client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshToken });

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (body is not null)
            request.Content = JsonContent.Create(body);

        return _client.SendAsync(request);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task<ErrorBody> AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Equal(code, error!.Code);
        Assert.NotEmpty(error.Message);
        Assert.NotEmpty(error.TraceId);
        return error;
    }

    private sealed record ErrorBody(string Code, string Message, string TraceId, Dictionary<string, string[]>? Errors);
}
