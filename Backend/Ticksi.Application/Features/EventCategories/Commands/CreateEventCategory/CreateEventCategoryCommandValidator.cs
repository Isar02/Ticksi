using FluentValidation;

namespace Ticksi.Application.Features.EventCategories.Commands.CreateEventCategory
{
    public class CreateEventCategoryCommandValidator : AbstractValidator<CreateEventCategoryCommand>
    {
        public CreateEventCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        }
    }
}

