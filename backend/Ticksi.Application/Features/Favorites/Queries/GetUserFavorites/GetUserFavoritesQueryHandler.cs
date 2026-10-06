using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Favorites.Queries.GetUserFavorites;

public class GetUserFavoritesQueryHandler : IRequestHandler<GetUserFavoritesQuery, List<Guid>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetUserFavoritesQueryHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<Guid>> Handle(GetUserFavoritesQuery request, CancellationToken cancellationToken)
    {
        var userPublicId = _currentUser.RequirePublicId();

        return await _context.Favorites
            .AsNoTracking()
            .Where(f => f.AppUser!.PublicId == userPublicId)
            .Select(f => f.Event!.PublicId)
            .ToListAsync(cancellationToken);
    }
}
