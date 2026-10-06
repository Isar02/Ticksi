using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetUsersQueryHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        await UserAdministrator.ResolveAsync(_context, _currentUser, cancellationToken);

        var users = _context.AppUsers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            users = users.Where(u =>
                (u.FirstName + " " + u.LastName).Contains(term) || u.Email.Contains(term));
        }

        if (request.RoleId.HasValue)
            users = users.Where(u => u.Role!.PublicId == request.RoleId.Value);

        if (request.IsActive.HasValue)
            users = users.Where(u => u.IsActive == request.IsActive.Value);

        if (request.RegisteredFrom is { } registeredFrom)
        {
            var from = registeredFrom.ToDateTime(TimeOnly.MinValue);
            users = users.Where(u => u.RegistrationDate >= from);
        }

        if (request.RegisteredTo is { } registeredTo)
        {
            var to = registeredTo.ToDateTime(TimeOnly.MaxValue);
            users = users.Where(u => u.RegistrationDate <= to);
        }

        return await Sort(users.Select(UserDto.Projection), request.SortBy, request.SortDescending)
            .ThenBy(u => u.PublicId)
            .ToPagedResultAsync(request.Page, request.PageSize, cancellationToken);
    }

    private static IOrderedQueryable<UserDto> Sort(IQueryable<UserDto> rows, string? sortBy, bool descending) =>
        sortBy?.ToLowerInvariant() switch
        {
            "email" => descending ? rows.OrderByDescending(u => u.Email) : rows.OrderBy(u => u.Email),
            "role" => descending ? rows.OrderByDescending(u => u.RoleName) : rows.OrderBy(u => u.RoleName),
            "registered" => descending
                ? rows.OrderByDescending(u => u.RegistrationDate)
                : rows.OrderBy(u => u.RegistrationDate),
            _ => descending
                ? rows.OrderByDescending(u => u.LastName).ThenByDescending(u => u.FirstName)
                : rows.OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
        };
}
