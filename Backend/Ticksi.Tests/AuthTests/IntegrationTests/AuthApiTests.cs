using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Auth.Queries.CheckEmailAvailability;
using Ticksi.Application.Interfaces;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.AuthTests.IntegrationTests;

public class AuthApiTests(TicksiApiFactory factory) : IClassFixture<TicksiApiFactory>
{
    private const string Password = "Secret123";

    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_NewEmail_ReturnsASessionThatOpensAProtectedEndpoint()
    {
        var request = NewUser();

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.Equal(request.Email, session!.Email);
        Assert.NotEmpty(session.AccessToken);
        Assert.NotEmpty(session.RefreshToken);

        var favorites = await GetFavoritesAsync(session.AccessToken);
        Assert.Equal(HttpStatusCode.OK, favorites.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/favorites");

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, "unauthorized");
    }

    [Fact]
    public async Task Register_InvalidInput_Returns400WithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", NewUser() with { Phone = "123", Password = "12345" });

        var error = await AssertErrorAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        Assert.Contains("phone", error.Errors!.Keys);
        Assert.Contains("password", error.Errors.Keys);
    }

    [Fact]
    public async Task Register_EmailTaken_Returns400OnEmail()
    {
        var request = NewUser();
        await RegisterAsync(request);

        var response = await _client.PostAsJsonAsync("/api/auth/register", request with { FirstName = "Other" });

        await AssertEmailTakenAsync(response);
    }

    [Fact]
    public async Task Register_SameEmailInParallel_OneSucceedsAndTheRestReturn400OnEmail()
    {
        var request = NewUser();

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 6).Select(_ => _client.PostAsJsonAsync("/api/auth/register", request)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
        foreach (var rejected in responses.Where(r => r.StatusCode != HttpStatusCode.OK))
            await AssertEmailTakenAsync(rejected);
    }

    [Fact]
    public async Task EmailAvailability_FreeAndTakenEmail_AnswersWithoutSignIn()
    {
        var request = NewUser();

        Assert.True(await IsEmailAvailableAsync(request.Email));

        await RegisterAsync(request);

        Assert.False(await IsEmailAvailableAsync($"  {request.Email} "));
    }

    [Theory]
    [InlineData("?email=not-an-email", "Invalid email format.")]
    [InlineData("?email=", "Email is required.")]
    [InlineData("", "Email is required.")]
    public async Task EmailAvailability_InvalidOrMissingEmail_Returns400OnEmail(string query, string message)
    {
        var response = await _client.GetAsync($"/api/auth/email-availability{query}");

        var error = await AssertErrorAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        Assert.Equal([message], error.Errors!["email"]);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsANewSession()
    {
        var request = NewUser();
        var registered = await RegisterAsync(request);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { request.Email, Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.Equal(registered.PublicId, session!.PublicId);
        Assert.NotEqual(registered.RefreshToken, session.RefreshToken);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var request = NewUser();
        await RegisterAsync(request);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { request.Email, Password = "WrongPassword1" });

        var error = await AssertErrorAsync(response, HttpStatusCode.Unauthorized, "unauthorized");
        Assert.Equal("Invalid email or password.", error.Message);
    }

    [Fact]
    public async Task Refresh_RotatesTheTokenAndAReusedTokenEndsTheSession()
    {
        var session = await RegisterAsync(NewUser());

        var refreshed = await RefreshAsync(session.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
        var rotated = await refreshed.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotEqual(session.RefreshToken, rotated!.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, (await GetFavoritesAsync(rotated.AccessToken)).StatusCode);

        await AssertErrorAsync(await RefreshAsync(session.RefreshToken), HttpStatusCode.Unauthorized, "unauthorized");
        await AssertErrorAsync(await RefreshAsync(rotated.RefreshToken), HttpStatusCode.Unauthorized, "unauthorized");
    }

    [Fact]
    public async Task SignInAndRefresh_EachStartAThirtyMinuteIdleWindow()
    {
        var before = DateTime.UtcNow;
        var session = await RegisterAsync(NewUser());
        var refreshed = await (await RefreshAsync(session.RefreshToken)).Content.ReadFromJsonAsync<AuthResponseDto>();
        var after = DateTime.UtcNow;

        Assert.All([session.RefreshTokenExpiresAtUtc, refreshed!.RefreshTokenExpiresAtUtc], expiresAt =>
            Assert.InRange(expiresAt, before.AddMinutes(30), after.AddMinutes(30)));
    }

    [Fact]
    public async Task Refresh_AfterTheIdleWindow_Returns401()
    {
        var session = await RegisterAsync(NewUser());

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var tokenHash = scope.ServiceProvider.GetRequiredService<IJwtTokenService>().HashRefreshToken(session.RefreshToken);
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await context.RefreshTokens.SingleAsync(t => t.TokenHash == tokenHash);
            stored.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(-1);
            await context.SaveChangesAsync();
        }

        await AssertErrorAsync(await RefreshAsync(session.RefreshToken), HttpStatusCode.Unauthorized, "unauthorized");
    }

    [Fact]
    public async Task Logout_EndsTheSessionAndCanBeRepeated()
    {
        var session = await RegisterAsync(NewUser());

        var logout = await _client.PostAsJsonAsync("/api/auth/logout", new { session.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        await AssertErrorAsync(await RefreshAsync(session.RefreshToken), HttpStatusCode.Unauthorized, "unauthorized");

        var repeated = await _client.PostAsJsonAsync("/api/auth/logout", new { session.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, repeated.StatusCode);
    }

    [Fact]
    public async Task Logout_EmptyToken_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/logout", new { RefreshToken = "" });

        var error = await AssertErrorAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        Assert.Contains("refreshToken", error.Errors!.Keys);
    }

    private static RegisterRequest NewUser() =>
        new("Ana", "Kovac", $"ana.{Guid.NewGuid():N}@ticksi.com", Password, "+387 61 123 456");

    private async Task<AuthResponseDto> RegisterAsync(RegisterRequest request)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponseDto>())!;
    }

    private async Task<bool> IsEmailAvailableAsync(string email)
    {
        var response = await _client.GetAsync($"/api/auth/email-availability?email={Uri.EscapeDataString(email)}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<EmailAvailabilityDto>())!.Available;
    }

    private static async Task AssertEmailTakenAsync(HttpResponseMessage response)
    {
        var error = await AssertErrorAsync(response, HttpStatusCode.BadRequest, "validation_failed");
        Assert.Equal(["An account with this email already exists."], error.Errors!["email"]);
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        _client.PostAsJsonAsync("/api/auth/refresh", new { RefreshToken = refreshToken });

    private Task<HttpResponseMessage> GetFavoritesAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/favorites");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return _client.SendAsync(request);
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

    private sealed record RegisterRequest(string FirstName, string LastName, string Email, string Password, string Phone);

    private sealed record ErrorBody(string Code, string Message, string TraceId, Dictionary<string, string[]>? Errors);
}
