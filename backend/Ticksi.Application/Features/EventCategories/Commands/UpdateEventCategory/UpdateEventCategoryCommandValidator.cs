using FluentValidation;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.EventCategories.Commands.UpdateEventCategory
{
    public class UpdateEventCategoryCommandValidator : AbstractValidator<UpdateEventCategoryCommand>
    {
        public UpdateEventCategoryCommandValidator()
        {
            // Note: PublicId is set from route parameter in controller, not from request body

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

