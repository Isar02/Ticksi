using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Payments;

internal static class BuyerOrders
{
    public static async Task<Order> FindAsync(
        IAppDbContext context,
        ICurrentUser currentUser,
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var userPublicId = currentUser.RequirePublicId();

        return await context.Orders
            .Include(o => o.Payment)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.PublicId == orderId && o.AppUser!.PublicId == userPublicId, cancellationToken)
            ?? throw new NotFoundException("Order not found.");
    }
}
