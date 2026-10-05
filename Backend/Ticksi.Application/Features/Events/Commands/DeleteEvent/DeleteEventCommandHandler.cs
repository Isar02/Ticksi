using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Events.Commands.DeleteEvent;

public class DeleteEventCommandHandler : IRequestHandler<DeleteEventCommand>
{
    private const string EventHasOrders = "The event has orders, so it cannot be deleted.";

    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public DeleteEventCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        var editor = await EventEditor.ResolveAsync(_context, _currentUser, cancellationToken);

        var item = await _context.Events
            .FirstOrDefaultAsync(e => e.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException("Event not found.");

        editor.EnsureCanManage(item);

        if (await HasOrdersAsync(item.Id, cancellationToken))
            throw new ConflictException(EventHasOrders);

        _context.Events.Remove(item);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // An order may have been placed between the check and the delete.
            if (!await HasOrdersAsync(item.Id, cancellationToken))
                throw;

            throw new ConflictException(EventHasOrders);
        }
    }

    private Task<bool> HasOrdersAsync(int eventId, CancellationToken cancellationToken) =>
        _context.OrderItems.AnyAsync(i => i.TicketType!.EventId == eventId, cancellationToken);
}
