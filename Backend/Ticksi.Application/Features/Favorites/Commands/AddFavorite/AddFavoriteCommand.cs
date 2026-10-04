using MediatR;

namespace Ticksi.Application.Features.Favorites.Commands.AddFavorite;

public class AddFavoriteCommand : IRequest
{
    public Guid UserPublicId { get; set; }
    public Guid EventPublicId { get; set; }
}
