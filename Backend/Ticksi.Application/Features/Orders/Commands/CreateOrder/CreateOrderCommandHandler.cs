using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private const int MaxReserveAttempts = 3;
    private const string TicketTypeGone = "One of the chosen ticket types no longer exists.";

    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly IEventClock _eventClock;

    public CreateOrderCommandHandler(IAppDbContext context, ICurrentUser currentUser, TimeProvider timeProvider, IEventClock eventClock)
    {
        _context = context;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _eventClock = eventClock;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var userPublicId = _currentUser.RequirePublicId();

        var userId = await _context.AppUsers
            .Where(u => u.PublicId == userPublicId)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedException("Your account could not be found.");

        var publicIds = request.Items.Select(i => i.TicketTypeId).ToList();
        var ticketTypes = await _context.TicketTypes
            .Include(t => t.Event)
            .Where(t => publicIds.Contains(t.PublicId))
            .ToDictionaryAsync(t => t.PublicId, cancellationToken);

        if (ticketTypes.Count != publicIds.Count)
            throw new NotFoundException(TicketTypeGone);

        var started = ticketTypes.Values.FirstOrDefault(t => t.Event!.Date <= _eventClock.Now);
        if (started is not null)
            throw new ConflictException($"Ticket sales for \"{started.Event!.Name}\" have closed.");

        var lines = request.Items.Select(i => (TicketType: ticketTypes[i.TicketTypeId], i.Quantity)).ToList();
        var orderId = await PlaceAsync(userId, lines, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == orderId)
            .Select(OrderDto.Projection)
            .SingleAsync(cancellationToken);
    }

    private async Task<int> PlaceAsync(
        int userId,
        List<(TicketType TicketType, int Quantity)> lines,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            await using var transaction = await _context.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var (ticketType, quantity) in lines)
                {
                    EnsureAvailable(ticketType, quantity);
                    ticketType.QuantityReserved += quantity;
                }

                await _context.SaveChangesAsync(cancellationToken);

                var order = new Order
                {
                    AppUserId = userId,
                    CreatedAtUtc = nowUtc,
                    TotalAmount = lines.Sum(l => l.TicketType.Price * l.Quantity),
                    Items = lines
                        .Select(l => new OrderItem { TicketTypeId = l.TicketType.Id, Quantity = l.Quantity, UnitPrice = l.TicketType.Price })
                        .ToList()
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return order.Id;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (attempt == MaxReserveAttempts)
                    throw new ConflictException("Tickets are selling fast right now. Please try again.");

                // Inventory changed: undo this attempt, then start again from the counts now stored.
                await transaction.RollbackAsync(cancellationToken);

                foreach (var (ticketType, quantity) in lines)
                    ticketType.QuantityReserved -= quantity;

                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(cancellationToken);
                    if (entry.State == EntityState.Detached)
                        throw new NotFoundException(TicketTypeGone);
                }
            }
        }
    }

    private static void EnsureAvailable(TicketType ticketType, int quantity)
    {
        var left = ticketType.Quantity - ticketType.QuantityReserved;
        if (left >= quantity)
            return;

        var eventName = ticketType.Event!.Name;
        throw new ConflictException(left <= 0
            ? $"\"{ticketType.Name}\" tickets for \"{eventName}\" are sold out."
            : $"Only {left} \"{ticketType.Name}\" tickets are left for \"{eventName}\".");
    }
}
