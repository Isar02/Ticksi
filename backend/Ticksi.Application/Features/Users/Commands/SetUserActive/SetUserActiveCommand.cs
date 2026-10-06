using MediatR;

namespace Ticksi.Application.Features.Users.Commands.SetUserActive;

public class SetUserActiveCommand : IRequest
{
    public Guid PublicId { get; set; }
    public bool IsActive { get; set; }
}
