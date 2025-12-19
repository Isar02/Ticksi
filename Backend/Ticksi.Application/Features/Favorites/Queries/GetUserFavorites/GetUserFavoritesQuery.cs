using MediatR;

namespace Ticksi.Application.Features.Favorites.Queries.GetUserFavorites
{
    public class GetUserFavoritesQuery : IRequest<List<Guid>>
    {
        public Guid UserPublicId { get; set; }
    }
}

