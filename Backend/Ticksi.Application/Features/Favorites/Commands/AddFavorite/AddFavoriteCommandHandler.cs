using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Favorites.Commands.AddFavorite;

public class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand>
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IAppDbContext _context;

    public AddFavoriteCommandHandler(IFavoriteRepository favoriteRepository, IAppDbContext context)
    {
        _favoriteRepository = favoriteRepository;
        _context = context;
    }

    public async Task Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.PublicId == request.UserPublicId, cancellationToken)
            ?? throw new UnauthorizedException("Your account could not be found.");

        var eventEntity = await _context.Events
            .FirstOrDefaultAsync(e => e.PublicId == request.EventPublicId, cancellationToken)
            ?? throw new NotFoundException("Event not found.");

        if (await _favoriteRepository.GetByUserAndEventAsync(request.UserPublicId, request.EventPublicId) != null)
            throw new ConflictException("This event is already in your favorites.");

        try
        {
            await _favoriteRepository.AddAsync(new Favorite { AppUserId = user.Id, EventId = eventEntity.Id });
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            throw new ConflictException("This event is already in your favorites.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("IX_Favorites_AppUserId_EventId", StringComparison.OrdinalIgnoreCase);
    }
}
