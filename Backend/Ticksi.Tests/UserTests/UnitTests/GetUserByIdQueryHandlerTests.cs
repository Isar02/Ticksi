using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Users;
using Ticksi.Application.Features.Users.Queries.GetUserById;

namespace Ticksi.Tests.UserTests.UnitTests;

public class GetUserByIdQueryHandlerTests : UserHandlerTestBase
{
    [Fact]
    public async Task Handle_ExistingAccount_ReturnsItsEditableFields()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var organizer = await AddUserAsync(Role.Names.Organizer, isActive: false);

        var user = await QueryAsync(admin, organizer.PublicId);

        Assert.Equal(
            (organizer.PublicId, "Lejla", "Begic", organizer.Email, "+387 61 123 456", OrganizerRoleId, Role.Names.Organizer, false),
            (user.PublicId, user.FirstName, user.LastName, user.Email, user.Phone, user.RoleId, user.RoleName, user.IsActive));
    }

    [Fact]
    public async Task Handle_UnknownAccount_ThrowsNotFound()
    {
        var admin = await AddUserAsync(Role.Names.Admin);

        await Assert.ThrowsAsync<NotFoundException>(() => QueryAsync(admin, Guid.NewGuid()));
    }

    [Fact]
    public async Task Handle_Organizer_ThrowsForbidden()
    {
        var organizer = await AddUserAsync(Role.Names.Organizer);

        await Assert.ThrowsAsync<ForbiddenException>(() => QueryAsync(organizer, organizer.PublicId));
    }

    private async Task<UserDto> QueryAsync(AppUser caller, Guid userId)
    {
        await using var context = Database.CreateContext();
        var handler = new GetUserByIdQueryHandler(context, SignedIn(caller));
        return await handler.Handle(new GetUserByIdQuery(userId), CancellationToken.None);
    }
}
