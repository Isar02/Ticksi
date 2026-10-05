using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Users.Queries.GetUserById;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetUserByIdQueryHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        await UserAdministrator.ResolveAsync(_context, _currentUser, cancellationToken);

        return await _context.AppUsers
            .AsNoTracking()
            .Where(u => u.PublicId == request.UserId)
            .Select(UserDto.Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("User not found.");
    }
}
