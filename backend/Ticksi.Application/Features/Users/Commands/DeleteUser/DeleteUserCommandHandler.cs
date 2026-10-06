using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private const string UserHasHistory =
        "This account has orders, events or reviews, so it cannot be deleted. Deactivate it instead.";

    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeleteUserCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.BeginUserAdministrationAsync(cancellationToken);
        var administrator = await UserAdministrator.ResolveAsync(_context, _currentUser, cancellationToken);

        var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        administrator.EnsureNotSelf(user, "You cannot delete your own account.");

        if (await HasHistoryAsync(user.Id, cancellationToken))
            throw new ConflictException(UserHasHistory);

        _context.Carts.RemoveRange(_context.Carts.Where(c => c.AppUserId == user.Id));
        _context.AppUsers.Remove(user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // An order, event or review may have been added between the check and the delete.
            if (!await HasHistoryAsync(user.Id, cancellationToken))
                throw;

            throw new ConflictException(UserHasHistory);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<bool> HasHistoryAsync(int userId, CancellationToken cancellationToken) =>
        await _context.Orders.AnyAsync(o => o.AppUserId == userId, cancellationToken)
        || await _context.Events.AnyAsync(e => e.AppUserId == userId, cancellationToken)
        || await _context.Reviews.AnyAsync(r => r.AppUserId == userId, cancellationToken);
}
