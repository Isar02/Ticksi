using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Events.Commands.UpdateEvent;

public class UpdateEventCommandHandler : IRequestHandler<UpdateEventCommand>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public UpdateEventCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        var editor = await EventEditor.ResolveAsync(_context, _currentUser, cancellationToken);

        var item = await _context.Events
            .Include(e => e.TicketTypes)
            .FirstOrDefaultAsync(e => e.PublicId == request.PublicId, cancellationToken)
            ?? throw new NotFoundException("Event not found.");

        editor.EnsureCanManage(item.AppUserId);

        try
        {
            await EventWriter.SaveAsync(_context, item, request, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // A purchase reserved tickets, the poster changed or the event was deleted between loading and saving.
            throw new ConflictException("The event changed while you were editing it. Reload it and try again.");
        }
    }
}
