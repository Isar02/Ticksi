using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Tickets;

internal static class BuyerTickets
{
    public static IQueryable<Ticket> OwnedBy(this IQueryable<Ticket> tickets, Guid userPublicId) =>
        tickets.Where(t => t.OrderItem!.Order!.AppUser!.PublicId == userPublicId);
}
