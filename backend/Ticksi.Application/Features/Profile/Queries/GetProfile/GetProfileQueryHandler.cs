using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Profile.Queries.GetProfile;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, ProfileDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetProfileQueryHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ProfileDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        return await ProfileOwner.Query(_context, _currentUser)
            .AsNoTracking()
            .Select(ProfileDto.Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedException(ProfileOwner.NotFoundMessage);
    }
}
