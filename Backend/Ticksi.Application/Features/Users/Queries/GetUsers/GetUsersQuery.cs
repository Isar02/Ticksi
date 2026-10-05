using MediatR;
using Ticksi.Application.Common;

namespace Ticksi.Application.Features.Users.Queries.GetUsers;

public class GetUsersQuery : IRequest<PagedResult<UserDto>>
{
    public string? Search { get; set; }
    public Guid? RoleId { get; set; }
    public bool? IsActive { get; set; }
    public DateOnly? RegisteredFrom { get; set; }
    public DateOnly? RegisteredTo { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
