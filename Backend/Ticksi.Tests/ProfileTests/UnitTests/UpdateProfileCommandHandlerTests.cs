using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Profile;
using Ticksi.Application.Features.Profile.Commands.UpdateProfile;
using Ticksi.Tests.AuthTests.UnitTests;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.ProfileTests.UnitTests;

public class UpdateProfileCommandHandlerTests : AuthHandlerTestBase
{
    [Fact]
    public async Task Handle_SignedInUser_SavesNamesAndPhoneAndKeepsTheRest()
    {
        var other = await AddUserAsync("other@ticksi.com");
        var user = await AddUserAsync();

        var profile = await HandleAsync(user.PublicId, Command());

        Assert.Equal(("Lejla", "Begic", "061-987-654", "ana@ticksi.com", Role.Names.User),
            (profile.FirstName, profile.LastName, profile.Phone, profile.Email, profile.Role));

        await using var context = Database.CreateContext();
        var stored = await context.AppUsers.SingleAsync(u => u.Id == user.Id);
        Assert.Equal(("Lejla", "Begic", "061-987-654", "ana@ticksi.com", user.PasswordHash),
            (stored.FirstName, stored.LastName, stored.Phone, stored.Email, stored.PasswordHash));
        Assert.Equal("Ana", (await context.AppUsers.SingleAsync(u => u.Id == other.Id)).FirstName);
    }

    [Fact]
    public async Task Handle_DeactivatedUser_ThrowsUnauthorizedAndSavesNothing()
    {
        var user = await AddUserAsync();
        await DeactivateAsync(user);

        await Assert.ThrowsAsync<UnauthorizedException>(() => HandleAsync(user.PublicId, Command()));

        await using var context = Database.CreateContext();
        Assert.Equal("Ana", (await context.AppUsers.SingleAsync(u => u.Id == user.Id)).FirstName);
    }

    private static UpdateProfileCommand Command() =>
        new() { FirstName = "Lejla", LastName = "Begic", Phone = "061-987-654" };

    private async Task<ProfileDto> HandleAsync(Guid publicId, UpdateProfileCommand command)
    {
        await using var context = Database.CreateContext();
        var handler = new UpdateProfileCommandHandler(context, new FakeCurrentUser(publicId));
        return await handler.Handle(command, CancellationToken.None);
    }
}
