using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;

namespace Ticksi.Application.Features.Auth.Commands.Refresh;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private const string InvalidTokenMessage = "The session has expired. Please sign in again.";

    private readonly IAppDbContext _context;
    private readonly IJwtTokenService _tokenService;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenCommandHandler(IAppDbContext context, IJwtTokenService tokenService, TimeProvider timeProvider)
    {
        _context = context;
        _tokenService = tokenService;
        _timeProvider = timeProvider;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _context.RefreshTokens
            .Include(t => t.AppUser!)
                .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken)
            ?? throw new UnauthorizedException(InvalidTokenMessage);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // A rotated token coming back means it was copied, so every session of the user ends.
        if (storedToken.RevokedReason == RefreshTokenRevocation.Rotated)
        {
            await RevokeAllActiveAsync(storedToken.AppUserId, now, cancellationToken);
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        if (storedToken.RevokedAtUtc is not null || storedToken.ExpiresAtUtc <= now)
            throw new UnauthorizedException(InvalidTokenMessage);

        storedToken.Revoke(RefreshTokenRevocation.Rotated, now);
        var response = AuthSession.Start(_context, _tokenService, storedToken.AppUser!);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        return response;
    }

    private async Task RevokeAllActiveAsync(int userId, DateTime now, CancellationToken cancellationToken)
    {
        while (true)
        {
            var activeTokens = await _context.RefreshTokens
                .Where(t => t.AppUserId == userId && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
                token.Revoke(RefreshTokenRevocation.ReuseDetected, now);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // A conflict is a row another request has just revoked; reloaded, it drops out of the next round.
                foreach (var entry in ex.Entries)
                    await entry.ReloadAsync(cancellationToken);
            }
        }
    }
}
