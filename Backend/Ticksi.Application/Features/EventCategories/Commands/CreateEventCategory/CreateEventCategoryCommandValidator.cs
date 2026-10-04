using FluentValidation;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.EventCategories.Commands.CreateEventCategory
{
    public class CreateEventCategoryCommandValidator : AbstractValidator<CreateEventCategoryCommand>
    {
        public CreateEventCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(EventCategory.Constraints.NameMaxLength)
                .WithMessage("Name cannot exceed {MaxLength} characters.");

            RuleFor(x => x.Description)
                .MaximumLength(EventCategory.Constraints.DescriptionMaxLength)
                .WithMessage("Description cannot exceed {MaxLength} characters.");

            RuleFor(x => x.PosterUrl)
                .MaximumLength(EventCategory.Constraints.PosterUrlMaxLength)
                .WithMessage("Poster URL cannot exceed {MaxLength} characters.");
        }
    }
}

