using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Ticksi.Application.Common;
using Ticksi.Application.DTOs;
using Ticksi.Application.Features.Auth;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Enums;

namespace Ticksi.Application.Features.Profile.Commands.ChangePassword;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, AuthResponseDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IJwtTokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;

    public ChangePasswordCommandHandler(
        IAppDbContext context,
        ICurrentUser currentUser,
        IJwtTokenService tokenService,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
    }

    public async Task<AuthResponseDto> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _context.BeginPasswordChangeAsync(_currentUser.RequirePublicId(), cancellationToken);
        var user = await ProfileOwner.LoadAsync(_context, _currentUser, cancellationToken);

        if (_passwordHasher.Verify(user, request.CurrentPassword) == PasswordCheck.Failed)
            throw new ValidationException([new ValidationFailure(nameof(ChangePasswordCommand.CurrentPassword), "The current password is incorrect.")]);

        user.PasswordHash = _passwordHasher.Hash(user, request.NewPassword);
        await UserSessions.EndAllAsync(_context, user.Id, RefreshTokenRevocation.PasswordChanged, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        var response = AuthSession.Start(_context, _tokenService, user);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return response;
    }
}
