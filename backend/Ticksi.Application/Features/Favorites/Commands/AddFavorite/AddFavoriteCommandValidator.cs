using FluentValidation;

namespace Ticksi.Application.Features.Favorites.Commands.AddFavorite;

public class AddFavoriteCommandValidator : AbstractValidator<AddFavoriteCommand>
{
    public AddFavoriteCommandValidator()
    {
        RuleFor(x => x.EventPublicId).NotEmpty().WithMessage("Event is required.");
    }
}
