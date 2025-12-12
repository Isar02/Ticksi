using FluentValidation;

namespace Ticksi.Application.Features.EventCategories.Commands.UpdateEventCategory
{
    public class UpdateEventCategoryCommandValidator : AbstractValidator<UpdateEventCategoryCommand>
    {
        public UpdateEventCategoryCommandValidator()
        {
            // Note: PublicId is set from route parameter in controller, not from request body

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        }
    }
}

