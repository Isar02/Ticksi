using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public UpdateUserCommandHandler(IAppDbContext context, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.BeginUserAdministrationAsync(cancellationToken);
        var administrator = await UserAdministrator.ResolveAsync(_context, _currentUser, cancellationToken);

        var user = await _context.AppUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (user.Role!.PublicId != request.RoleId)
            administrator.EnsureNotSelf(user, "You cannot change your own role.");
        if (!request.IsActive)
            administrator.EnsureNotSelf(user, "You cannot deactivate your own account.");

        var wasActive = user.IsActive;
        await UserWriter.ApplyAsync(_context, user, request, cancellationToken);
        await UserWriter.SaveAsync(_context, user, wasActive, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
