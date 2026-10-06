using MediatR;

namespace Ticksi.Application.Features.Users.Queries.GetRoles;

public record GetRolesQuery : IRequest<List<RoleDto>>;
