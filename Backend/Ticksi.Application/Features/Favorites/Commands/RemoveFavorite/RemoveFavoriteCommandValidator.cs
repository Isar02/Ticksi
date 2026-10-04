using FluentValidation;

namespace Ticksi.Application.Features.Favorites.Commands.RemoveFavorite;

public class RemoveFavoriteCommandValidator : AbstractValidator<RemoveFavoriteCommand>
{
    public RemoveFavoriteCommandValidator()
    {
        RuleFor(x => x.EventPublicId).NotEmpty().WithMessage("Event is required.");
        RuleFor(x => x.UserPublicId).NotEmpty().WithMessage("User is required.");
    }
}
