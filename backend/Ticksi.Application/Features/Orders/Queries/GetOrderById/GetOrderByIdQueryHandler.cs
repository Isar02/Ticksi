using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public GetOrderByIdQueryHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var userPublicId = _currentUser.RequirePublicId();

        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.PublicId == request.OrderId && o.AppUser!.PublicId == userPublicId)
            .Select(OrderDto.Projection)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Order not found.");
    }
}
