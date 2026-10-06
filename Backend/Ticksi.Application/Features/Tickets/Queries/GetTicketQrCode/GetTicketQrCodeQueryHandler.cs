using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Tickets.Queries.GetTicketQrCode;

public class GetTicketQrCodeQueryHandler : IRequestHandler<GetTicketQrCodeQuery, byte[]>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IQrCodeGenerator _qrCodes;

    public GetTicketQrCodeQueryHandler(IAppDbContext context, ICurrentUser currentUser, IQrCodeGenerator qrCodes)
    {
        _context = context;
        _currentUser = currentUser;
        _qrCodes = qrCodes;
    }

    public async Task<byte[]> Handle(GetTicketQrCodeQuery request, CancellationToken cancellationToken)
    {
        var userPublicId = _currentUser.RequirePublicId();

        var code = await _context.Tickets
            .AsNoTracking()
            .OwnedBy(userPublicId)
            .Where(t => t.PublicId == request.TicketId)
            .Select(t => t.Code)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Ticket not found.");

        return _qrCodes.RenderPng(code);
    }
}
