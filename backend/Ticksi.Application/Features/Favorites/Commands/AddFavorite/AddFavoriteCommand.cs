using MediatR;

namespace Ticksi.Application.Features.Favorites.Commands.AddFavorite;

public class AddFavoriteCommand : IRequest
{
    public Guid EventPublicId { get; set; }
}
