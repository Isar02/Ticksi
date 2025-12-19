using MediatR;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Favorites.Queries.GetUserFavorites
{
    public class GetUserFavoritesQueryHandler : IRequestHandler<GetUserFavoritesQuery, List<Guid>>
    {
        private readonly IFavoriteRepository _favoriteRepository;

        public GetUserFavoritesQueryHandler(IFavoriteRepository favoriteRepository)
        {
            _favoriteRepository = favoriteRepository;
        }

        public async Task<List<Guid>> Handle(GetUserFavoritesQuery request, CancellationToken cancellationToken)
        {
            return await _favoriteRepository.GetUserFavoritesAsync(request.UserPublicId);
        }
    }
}

