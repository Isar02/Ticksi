using FluentValidation;

namespace Ticksi.Application.Features.Favorites.Queries.GetUserFavorites;

public class GetUserFavoritesQueryValidator : AbstractValidator<GetUserFavoritesQuery>
{
    public GetUserFavoritesQueryValidator()
    {
        RuleFor(x => x.UserPublicId).NotEmpty().WithMessage("User is required.");
    }
}
