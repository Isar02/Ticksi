using FluentValidation;
using API.DTOs;

namespace API.Validators
{
    public class EventCategoryUpdateDtoValidator : AbstractValidator<EventCategoryCreateDto>
    {
        public EventCategoryUpdateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name cannot exceed 100 characters.");
            RuleFor(x => x.Description)
                .MaximumLength(500).WithMessage("Description cannot exceed 500 characters.");
        }

    }
}
