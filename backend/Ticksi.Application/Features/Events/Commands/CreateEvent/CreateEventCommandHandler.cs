using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Events.Commands.CreateEvent;

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, EventReadDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public CreateEventCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<EventReadDto> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        var editor = await EventEditor.ResolveAsync(_context, _currentUser, cancellationToken);

        var item = new Event { AppUserId = editor.UserId };
        _context.Events.Add(item);
        await EventWriter.SaveAsync(_context, item, request, cancellationToken);

        return await _context.Events
            .AsNoTracking()
            .Where(e => e.Id == item.Id)
            .Select(EventProjections.ToReadDto)
            .SingleAsync(cancellationToken);
    }
}
