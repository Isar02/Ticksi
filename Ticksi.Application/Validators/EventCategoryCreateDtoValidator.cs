using FluentValidation;
using Ticksi.Application.DTOs;

namespace Ticksi.Application.Validators
{
    public class EventCategoryCreateDtoValidator : AbstractValidator<EventCategoryCreateDto>
    {
        public EventCategoryCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        }

    }
}
