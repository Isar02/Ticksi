using FluentValidation;

namespace Ticksi.Application.Features.Favorites.Commands.AddFavorite
{
    public class AddFavoriteCommandValidator : AbstractValidator<AddFavoriteCommand>
    {
        public AddFavoriteCommandValidator()
        {
            RuleFor(x => x.EventPublicId)
                .NotEmpty().WithMessage("EventPublicId is required.");

            RuleFor(x => x.UserPublicId)
                .NotEmpty().WithMessage("UserPublicId is required.");
        }
    }
}

