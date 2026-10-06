using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Users.Queries.GetRoles;

namespace Ticksi.Tests.UserTests.UnitTests;

public class GetRolesQueryHandlerTests : UserHandlerTestBase
{
    [Fact]
    public async Task Handle_Admin_ReturnsEveryRoleByName()
    {
        var admin = await AddUserAsync(Role.Names.Admin);

        var roles = await QueryAsync(admin);

        Assert.Equal(
            [
                new RoleDto(AdminRoleId, Role.Names.Admin),
                new RoleDto(OrganizerRoleId, Role.Names.Organizer),
                new RoleDto(UserRoleId, Role.Names.User)
            ],
            roles);
    }

    [Fact]
    public async Task Handle_User_ThrowsForbidden()
    {
        var user = await AddUserAsync(Role.Names.User);

        await Assert.ThrowsAsync<ForbiddenException>(() => QueryAsync(user));
    }

    private async Task<List<RoleDto>> QueryAsync(AppUser caller)
    {
        await using var context = Database.CreateContext();
        var handler = new GetRolesQueryHandler(context, SignedIn(caller));
        return await handler.Handle(new GetRolesQuery(), CancellationToken.None);
    }
}
