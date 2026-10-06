using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Profile;
using Ticksi.Application.Features.Profile.Queries.GetProfile;
using Ticksi.Tests.AuthTests.UnitTests;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.ProfileTests.UnitTests;

public class GetProfileQueryHandlerTests : AuthHandlerTestBase
{
    [Fact]
    public async Task Handle_SignedInUser_ReturnsOwnDetails()
    {
        await AddUserAsync("other@ticksi.com");
        var user = await AddUserAsync();

        var profile = await HandleAsync(user.PublicId);

        Assert.Equal(("Ana", "Kovac", "ana@ticksi.com", "+387 61 123 456", Role.Names.User),
            (profile.FirstName, profile.LastName, profile.Email, profile.Phone, profile.Role));
        Assert.Equal(user.RegistrationDate, profile.RegistrationDate);
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ThrowsUnauthorized()
    {
        var user = await AddUserAsync();
        await DeactivateAsync(user);

        await Assert.ThrowsAsync<UnauthorizedException>(() => HandleAsync(user.PublicId));
    }

    [Fact]
    public async Task Handle_UnknownUser_ThrowsUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => HandleAsync(Guid.NewGuid()));
    }

    private async Task<ProfileDto> HandleAsync(Guid publicId)
    {
        await using var context = Database.CreateContext();
        var handler = new GetProfileQueryHandler(context, new FakeCurrentUser(publicId));
        return await handler.Handle(new GetProfileQuery(), CancellationToken.None);
    }
}
