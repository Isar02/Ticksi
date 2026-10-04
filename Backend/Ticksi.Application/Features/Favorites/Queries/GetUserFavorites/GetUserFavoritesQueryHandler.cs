using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Favorites.Queries.GetUserFavorites;

public class GetUserFavoritesQueryHandler : IRequestHandler<GetUserFavoritesQuery, List<Guid>>
{
    private readonly IAppDbContext _context;

    public GetUserFavoritesQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Guid>> Handle(GetUserFavoritesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Favorites
            .AsNoTracking()
            .Where(f => f.AppUser!.PublicId == request.UserPublicId)
            .Select(f => f.Event!.PublicId)
            .ToListAsync(cancellationToken);
    }
}
