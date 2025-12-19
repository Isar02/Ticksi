using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Favorites.Commands.AddFavorite
{
    public class AddFavoriteCommandHandler : IRequestHandler<AddFavoriteCommand, bool>
    {
        private readonly IFavoriteRepository _favoriteRepository;
        private readonly IAppDbContext _context;

        public AddFavoriteCommandHandler(IFavoriteRepository favoriteRepository, IAppDbContext context)
        {
            _favoriteRepository = favoriteRepository;
            _context = context;
        }

        public async Task<bool> Handle(AddFavoriteCommand request, CancellationToken cancellationToken)
        {
            // Check if favorite already exists
            var existingFavorite = await _favoriteRepository.GetByUserAndEventAsync(
                request.UserPublicId, 
                request.EventPublicId);

            if (existingFavorite != null)
            {
                // Already exists, return false to indicate duplicate
                return false;
            }

            // Get the actual AppUser and Event entities by their PublicIds
            var user = await _context.AppUsers
                .FirstOrDefaultAsync(u => u.PublicId == request.UserPublicId, cancellationToken);
            
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.PublicId == request.EventPublicId, cancellationToken);

            if (user == null || eventEntity == null)
            {
                // User or Event not found
                return false;
            }

            // Create new favorite
            var favorite = new Favorite
            {
                AppUserId = user.Id,
                EventId = eventEntity.Id
            };

            await _favoriteRepository.AddAsync(favorite);
            return true;
        }
    }
}

