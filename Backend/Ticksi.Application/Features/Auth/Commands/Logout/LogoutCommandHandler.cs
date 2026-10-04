using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;

namespace Ticksi.Application.Features.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IAppDbContext _context;
    private readonly IJwtTokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public LogoutCommandHandler(IAppDbContext context, IJwtTokenService tokenService, TimeProvider timeProvider)
    {
        _context = context;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAtUtc == null, cancellationToken);

        if (storedToken is null)
            return;

        storedToken.Revoke(RefreshTokenRevocation.SignedOut, _timeProvider.GetUtcNow().UtcDateTime);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Revoked by a parallel request in the meantime; signing out is already done.
        }
    }
}
