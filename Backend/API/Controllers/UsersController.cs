using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.Features.Users;
using Ticksi.Application.Features.Users.Commands.CreateUser;
using Ticksi.Application.Features.Users.Commands.DeleteUser;
using Ticksi.Application.Features.Users.Commands.SetUserActive;
using Ticksi.Application.Features.Users.Commands.UpdateUser;
using Ticksi.Domain.Entities;

namespace API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = Role.Names.Admin)]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDto>> Create(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, dto);
    }

    [HttpPut("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Update(Guid userId, UpdateUserCommand command, CancellationToken cancellationToken)
    {
        command.PublicId = userId;
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPut("{userId:guid}/active")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> SetActive(Guid userId, SetUserActiveCommand command, CancellationToken cancellationToken)
    {
        command.PublicId = userId;
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Delete(Guid userId, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteUserCommand { PublicId = userId }, cancellationToken);
        return NoContent();
    }
}
