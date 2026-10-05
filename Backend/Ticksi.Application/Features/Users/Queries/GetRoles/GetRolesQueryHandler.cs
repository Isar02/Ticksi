using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Users.Queries.GetRoles;

public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RoleDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetRolesQueryHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<RoleDto>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        await UserAdministrator.ResolveAsync(_context, _currentUser, cancellationToken);

        return await _context.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.PublicId, r.Name))
            .ToListAsync(cancellationToken);
    }
}
