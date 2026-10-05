using Ticksi.Application.Features.Auth.Queries.CheckEmailAvailability;

namespace Ticksi.Tests.AuthTests.UnitTests;

public class CheckEmailAvailabilityQueryHandlerTests : AuthHandlerTestBase
{
    [Fact]
    public async Task Handle_UnusedEmail_IsAvailable()
    {
        await AddUserAsync("ana@ticksi.com");

        var result = await CheckAsync("marko@ticksi.com");

        Assert.True(result.Available);
    }

    [Theory]
    [InlineData("ana@ticksi.com")]
    [InlineData("  ana@ticksi.com ")]
    public async Task Handle_EmailOfAnExistingAccount_IsNotAvailable(string email)
    {
        await AddUserAsync("ana@ticksi.com");

        var result = await CheckAsync(email);

        Assert.False(result.Available);
    }

    [Fact]
    public async Task Handle_EmailOfADeactivatedAccount_IsNotAvailable()
    {
        await DeactivateAsync(await AddUserAsync("ana@ticksi.com"));

        var result = await CheckAsync("ana@ticksi.com");

        Assert.False(result.Available);
    }

    private async Task<EmailAvailabilityDto> CheckAsync(string email)
    {
        await using var context = Database.CreateContext();
        var handler = new CheckEmailAvailabilityQueryHandler(context);

        return await handler.Handle(new CheckEmailAvailabilityQuery(email), CancellationToken.None);
    }
}
