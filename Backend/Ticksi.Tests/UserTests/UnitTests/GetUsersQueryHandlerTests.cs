using Ticksi.Application.Common;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Users;
using Ticksi.Application.Features.Users.Queries.GetUsers;

namespace Ticksi.Tests.UserTests.UnitTests;

public class GetUsersQueryHandlerTests : UserHandlerTestBase
{
    [Fact]
    public async Task Handle_Search_MatchesFullNameOrEmail()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        await AddNamedUserAsync("Amar", "Hadzic", "amar@ticksi.com");
        await AddNamedUserAsync("Lana", "Kovac", "lana@mail.ba");
        await AddNamedUserAsync("Edin", "Hadzic", "edin@ticksi.com");

        var byFullName = await QueryAsync(admin, new GetUsersQuery { Search = " Amar Hadzic " });
        var byLastName = await QueryAsync(admin, new GetUsersQuery { Search = "Hadzic" });
        var byEmail = await QueryAsync(admin, new GetUsersQuery { Search = "mail.ba" });

        Assert.Equal(["amar@ticksi.com"], byFullName.Items.Select(u => u.Email));
        Assert.Equal(["Amar", "Edin"], byLastName.Items.Select(u => u.FirstName));
        Assert.Equal(["lana@mail.ba"], byEmail.Items.Select(u => u.Email));
    }

    [Fact]
    public async Task Handle_RoleAndActiveFilters_NarrowTheList()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        var organizer = await AddUserAsync(Role.Names.Organizer);
        var inactiveOrganizer = await AddUserAsync(Role.Names.Organizer, isActive: false);
        await AddUserAsync(Role.Names.User);

        var organizers = await QueryAsync(admin, new GetUsersQuery { RoleId = OrganizerRoleId });
        var inactive = await QueryAsync(admin, new GetUsersQuery { IsActive = false });
        var activeOrganizers = await QueryAsync(admin, new GetUsersQuery { RoleId = OrganizerRoleId, IsActive = true });

        Assert.Equal(
            new[] { organizer.PublicId, inactiveOrganizer.PublicId }.Order(),
            organizers.Items.Select(u => u.PublicId).Order());
        Assert.Equal([inactiveOrganizer.PublicId], inactive.Items.Select(u => u.PublicId));
        Assert.Equal([organizer.PublicId], activeOrganizers.Items.Select(u => u.PublicId));
    }

    [Fact]
    public async Task Handle_RegistrationRange_IncludesTheWholeLastDay()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        await AddNamedUserAsync("Before", "User", "before@ticksi.com", new DateTime(2026, 8, 31, 23, 59, 0));
        await AddNamedUserAsync("First", "Day", "first@ticksi.com", new DateTime(2026, 9, 1, 0, 0, 0));
        await AddNamedUserAsync("Last", "Evening", "last@ticksi.com", new DateTime(2026, 9, 30, 23, 30, 0));
        await AddNamedUserAsync("After", "User", "after@ticksi.com", new DateTime(2026, 10, 1, 0, 0, 0));

        var result = await QueryAsync(admin, new GetUsersQuery
        {
            RoleId = UserRoleId,
            RegisteredFrom = new DateOnly(2026, 9, 1),
            RegisteredTo = new DateOnly(2026, 9, 30),
            SortBy = "registered"
        });

        Assert.Equal(["First", "Last"], result.Items.Select(u => u.FirstName));
    }

    [Theory]
    [InlineData("name", false, new[] { "Begic", "Hadzic", "Kovac" })]
    [InlineData("NAME", true, new[] { "Kovac", "Hadzic", "Begic" })]
    [InlineData("email", false, new[] { "Kovac", "Begic", "Hadzic" })]
    [InlineData("email", true, new[] { "Hadzic", "Begic", "Kovac" })]
    [InlineData("registered", false, new[] { "Hadzic", "Kovac", "Begic" })]
    [InlineData("registered", true, new[] { "Begic", "Kovac", "Hadzic" })]
    [InlineData(null, false, new[] { "Begic", "Hadzic", "Kovac" })]
    public async Task Handle_Sort_OrdersByTheChosenField(string? sortBy, bool descending, string[] expected)
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        await AddNamedUserAsync("Lejla", "Begic", "b@ticksi.com", new DateTime(2026, 3, 1));
        await AddNamedUserAsync("Amar", "Hadzic", "c@ticksi.com", new DateTime(2026, 1, 1));
        await AddNamedUserAsync("Lana", "Kovac", "a@ticksi.com", new DateTime(2026, 2, 1));

        var result = await QueryAsync(admin, new GetUsersQuery { RoleId = UserRoleId, SortBy = sortBy, SortDescending = descending });

        Assert.Equal(expected, result.Items.Select(u => u.LastName));
    }

    [Fact]
    public async Task Handle_SortByName_UsesTheFirstNameWithinTheSameLastName()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        await AddNamedUserAsync("Edin", "Hadzic", "edin@ticksi.com");
        await AddNamedUserAsync("Amar", "Hadzic", "amar@ticksi.com");

        var result = await QueryAsync(admin, new GetUsersQuery { RoleId = UserRoleId, SortBy = "name", SortDescending = true });

        Assert.Equal(["Edin", "Amar"], result.Items.Select(u => u.FirstName));
    }

    [Fact]
    public async Task Handle_SortByRole_OrdersByRoleName()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        await AddUserAsync(Role.Names.User);
        await AddUserAsync(Role.Names.Organizer);

        var ascending = await QueryAsync(admin, new GetUsersQuery { SortBy = "role" });
        var descending = await QueryAsync(admin, new GetUsersQuery { SortBy = "role", SortDescending = true });

        Assert.Equal([Role.Names.Admin, Role.Names.Organizer, Role.Names.User], ascending.Items.Select(u => u.RoleName));
        Assert.Equal([Role.Names.User, Role.Names.Organizer, Role.Names.Admin], descending.Items.Select(u => u.RoleName));
    }

    [Fact]
    public async Task Handle_Paging_ReturnsTheRequestedPageWithTheRoleAndStatus()
    {
        var admin = await AddUserAsync(Role.Names.Admin);
        foreach (var name in new[] { "Avdic", "Basic", "Cehic", "Dedic" })
            await AddNamedUserAsync("Mina", name, $"{name.ToLowerInvariant()}@ticksi.com");

        var result = await QueryAsync(admin, new GetUsersQuery { RoleId = UserRoleId, Page = 2, PageSize = 3 });

        var row = Assert.Single(result.Items);
        Assert.Equal(("Dedic", UserRoleId, Role.Names.User, true), (row.LastName, row.RoleId, row.RoleName, row.IsActive));
        Assert.Equal((4, 2), (result.TotalCount, result.TotalPages));
    }

    [Theory]
    [InlineData(Role.Names.Organizer)]
    [InlineData(Role.Names.User)]
    public async Task Handle_NotAnAdministrator_ThrowsForbidden(string role)
    {
        var caller = await AddUserAsync(role);

        await Assert.ThrowsAsync<ForbiddenException>(() => QueryAsync(caller, new GetUsersQuery()));
    }

    [Fact]
    public async Task Handle_DeactivatedAdministrator_ThrowsUnauthorized()
    {
        var admin = await AddUserAsync(Role.Names.Admin, isActive: false);

        await Assert.ThrowsAsync<UnauthorizedException>(() => QueryAsync(admin, new GetUsersQuery()));
    }

    private async Task AddNamedUserAsync(string firstName, string lastName, string email, DateTime? registered = null)
    {
        await using var context = Database.CreateContext();
        context.AppUsers.Add(new AppUser
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            Phone = "+387 61 123 456",
            PasswordHash = "hash",
            RegistrationDate = registered ?? Now,
            Role = await context.Roles.SingleAsync(r => r.Name == Role.Names.User)
        });
        await context.SaveChangesAsync();
    }

    private async Task<PagedResult<UserDto>> QueryAsync(AppUser caller, GetUsersQuery query)
    {
        await using var context = Database.CreateContext();
        var handler = new GetUsersQueryHandler(context, SignedIn(caller));
        return await handler.Handle(query, CancellationToken.None);
    }
}
