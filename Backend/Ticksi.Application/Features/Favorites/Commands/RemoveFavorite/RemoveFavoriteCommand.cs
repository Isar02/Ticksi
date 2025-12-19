using MediatR;

namespace Ticksi.Application.Features.Favorites.Commands.RemoveFavorite
{
    public class RemoveFavoriteCommand : IRequest<bool>
    {
        public Guid UserPublicId { get; set; }
        public Guid EventPublicId { get; set; }
    }
}

