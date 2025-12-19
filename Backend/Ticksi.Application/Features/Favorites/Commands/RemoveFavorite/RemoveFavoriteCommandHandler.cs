using MediatR;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Favorites.Commands.RemoveFavorite
{
    public class RemoveFavoriteCommandHandler : IRequestHandler<RemoveFavoriteCommand, bool>
    {
        private readonly IFavoriteRepository _favoriteRepository;

        public RemoveFavoriteCommandHandler(IFavoriteRepository favoriteRepository)
        {
            _favoriteRepository = favoriteRepository;
        }

        public async Task<bool> Handle(RemoveFavoriteCommand request, CancellationToken cancellationToken)
        {
            // Find the favorite
            var favorite = await _favoriteRepository.GetByUserAndEventAsync(
                request.UserPublicId, 
                request.EventPublicId);

            if (favorite == null)
            {
                // Favorite not found
                return false;
            }

            // Remove the favorite
            await _favoriteRepository.DeleteAsync(favorite);
            return true;
        }
    }
}

