using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private const string EmailTaken = "An account with this email already exists.";

    private readonly IAppDbContext _context;
    private readonly IJwtTokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;

    public RegisterCommandHandler(
        IAppDbContext context,
        IJwtTokenService tokenService,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        if (await EmailExistsAsync(email, cancellationToken))
            throw EmailTakenError();

        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == Role.Names.User, cancellationToken)
            ?? throw new InvalidOperationException("The User role has not been seeded.");

        var user = new AppUser
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            RegistrationDate = _timeProvider.GetUtcNow().UtcDateTime,
            Role = defaultRole
        };
        user.PasswordHash = _passwordHasher.Hash(user, request.Password);

        _context.AppUsers.Add(user);
        var response = AuthSession.Start(_context, _tokenService, user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A parallel registration may have taken the email between the check and the save.
            if (!await EmailExistsAsync(email, cancellationToken))
                throw;

            throw EmailTakenError();
        }

        return response;
    }

    private static ValidationException EmailTakenError() =>
        new([new ValidationFailure(nameof(RegisterCommand.Email), EmailTaken)]);

    private Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken) =>
        _context.AppUsers.AnyAsync(u => u.Email == email, cancellationToken);
}
