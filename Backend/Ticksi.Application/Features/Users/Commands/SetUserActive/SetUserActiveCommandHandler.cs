using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Users.Commands.SetUserActive;

public class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;

    public SetUserActiveCommandHandler(IAppDbContext context, ICurrentUser currentUser, TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public async Task Handle(SetUserActiveCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.BeginUserAdministrationAsync(cancellationToken);
        var administrator = await UserAdministrator.ResolveAsync(_context, _currentUser, cancellationToken);

        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (!request.IsActive)
            administrator.EnsureNotSelf(user, "You cannot deactivate your own account.");

        var wasActive = user.IsActive;
        user.IsActive = request.IsActive;
        await UserWriter.SaveAsync(_context, user, wasActive, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
