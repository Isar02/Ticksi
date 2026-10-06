using MediatR;

namespace Ticksi.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommand : UserInput, IRequest
{
    public Guid PublicId { get; set; }
}
