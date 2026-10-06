using FluentValidation;
using Ticksi.Application.Features.Auth;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Profile.Commands.UpdateProfile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FirstName).PersonName("First name", AppUser.Constraints.FirstNameMaxLength);

        RuleFor(x => x.LastName).PersonName("Last name", AppUser.Constraints.LastNameMaxLength);

        RuleFor(x => x.Phone).PhoneNumber();
    }
}
