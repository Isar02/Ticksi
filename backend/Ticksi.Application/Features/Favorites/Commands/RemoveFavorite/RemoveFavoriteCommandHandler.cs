using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Favorites.Commands.RemoveFavorite;

public class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public RemoveFavoriteCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
    {
        var userPublicId = _currentUser.RequirePublicId();

        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f =>
                f.AppUser!.PublicId == userPublicId &&
                f.Event!.PublicId == request.EventPublicId,
                cancellationToken)
            ?? throw new NotFoundException("This event is not in your favorites.");

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
