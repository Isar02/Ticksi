using Ticksi.Application.Common;
using Ticksi.Application.Features.Users.Queries.GetUsers;

namespace Ticksi.Tests.UserTests.UnitTests;

public class GetUsersQueryValidatorTests
{
    private readonly GetUsersQueryValidator _validator = new();

    [Fact]
    public async Task Validate_EveryFilterAndSortSet_Passes()
    {
        var query = new GetUsersQuery
        {
            Search = "amar@ticksi.com",
            RoleId = Guid.NewGuid(),
            IsActive = false,
            RegisteredFrom = new DateOnly(2026, 9, 1),
            RegisteredTo = new DateOnly(2026, 9, 1),
            SortBy = "Registered",
            SortDescending = true,
            Page = 2,
            PageSize = PagingExtensions.MaxPageSize
        };

        var result = await _validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_UnknownValuesAndOversizedPage_ReportsEachField()
    {
        var query = new GetUsersQuery
        {
            Search = new string('a', AppUser.Constraints.EmailMaxLength + 1),
            RegisteredFrom = new DateOnly(2026, 9, 2),
            RegisteredTo = new DateOnly(2026, 9, 1),
            SortBy = "phone",
            Page = 0,
            PageSize = PagingExtensions.MaxPageSize + 1
        };

        var result = await _validator.ValidateAsync(query);

        Assert.Equal(
            ["Page", "PageSize", "RegisteredTo", "Search", "SortBy"],
            result.Errors.Select(e => e.PropertyName).Order());
    }
}
