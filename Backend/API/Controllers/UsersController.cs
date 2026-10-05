using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ticksi.Application.Common;
using Ticksi.Application.Features.Users;
using Ticksi.Application.Features.Users.Commands.CreateUser;
using Ticksi.Application.Features.Users.Commands.DeleteUser;
using Ticksi.Application.Features.Users.Commands.SetUserActive;
using Ticksi.Application.Features.Users.Commands.UpdateUser;
using Ticksi.Application.Features.Users.Queries.GetRoles;
using Ticksi.Application.Features.Users.Queries.GetUserById;
using Ticksi.Application.Features.Users.Queries.GetUsers;
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

    [HttpGet]
    public async Task<ActionResult<PagedResult<UserDto>>> GetAll(
        [FromQuery] GetUsersQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(query, cancellationToken));
    }

    [HttpGet("roles")]
    public async Task<ActionResult<List<RoleDto>>> GetRoles(CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetRolesQuery(), cancellationToken));
    }

    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(Guid userId, CancellationToken cancellationToken)
    {
        return Ok(await _mediator.Send(new GetUserByIdQuery(userId), cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<UserDto>> Create(CreateUserCommand command, CancellationToken cancellationToken)
    {
        var dto = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { userId = dto.PublicId }, dto);
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
