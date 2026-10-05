using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Events;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Favorites.Queries.GetFavoriteEvents;

public class GetFavoriteEventsQueryHandler : IRequestHandler<GetFavoriteEventsQuery, List<EventReadDto>>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetFavoriteEventsQueryHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    // Favorites carry no timestamp; the identity key grows with every new one, so it orders them by when they were added.
    public async Task<List<EventReadDto>> Handle(GetFavoriteEventsQuery request, CancellationToken cancellationToken)
    {
        var userPublicId = _currentUser.RequirePublicId();

        return await _context.Favorites
            .AsNoTracking()
            .Where(f => f.AppUser!.PublicId == userPublicId)
            .OrderByDescending(f => f.Id)
            .Select(f => f.Event!)
            .Select(EventProjections.ToReadDto)
            .ToListAsync(cancellationToken);
    }
}
