using MediatR;

namespace Ticksi.Application.Features.Users.Commands.DeleteUser;

public class DeleteUserCommand : IRequest
{
    public Guid PublicId { get; set; }
}
