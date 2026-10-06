using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Favorites.Commands.AddFavorite;

public class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand>
{
    private const string AlreadyFavorite = "This event is already in your favorites.";

    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public AddFavoriteCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
    {
        var userPublicId = _currentUser.RequirePublicId();

        var userId = await _context.AppUsers
            .Where(u => u.PublicId == userPublicId)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedException("Your account could not be found.");

        var eventId = await _context.Events
            .Where(e => e.PublicId == request.EventPublicId)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Event not found.");

        if (await IsFavoriteAsync(userId, eventId, cancellationToken))
            throw new ConflictException(AlreadyFavorite);

        _context.Favorites.Add(new Favorite { AppUserId = userId, EventId = eventId });

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A parallel request may have added the same favorite between the check and the save.
            if (!await IsFavoriteAsync(userId, eventId, cancellationToken))
                throw;

            throw new ConflictException(AlreadyFavorite);
        }
    }

    private Task<bool> IsFavoriteAsync(int userId, int eventId, CancellationToken cancellationToken) =>
        _context.Favorites.AnyAsync(f => f.AppUserId == userId && f.EventId == eventId, cancellationToken);
}
