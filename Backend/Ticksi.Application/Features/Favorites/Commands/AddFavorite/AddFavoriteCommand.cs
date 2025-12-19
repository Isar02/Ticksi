using MediatR;

namespace Ticksi.Application.Features.Favorites.Commands.AddFavorite
{
    public class AddFavoriteCommand : IRequest<bool>
    {
        public Guid UserPublicId { get; set; }
        public Guid EventPublicId { get; set; }
    }
}

