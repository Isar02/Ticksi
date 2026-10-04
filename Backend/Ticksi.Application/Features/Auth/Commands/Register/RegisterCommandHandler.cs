using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly IAppDbContext _context;
    private readonly IJwtTokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterCommandHandler(IAppDbContext context, IJwtTokenService tokenService, IPasswordHasher passwordHasher)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _context.AppUsers.AnyAsync(u => u.Email == request.Email, cancellationToken))
            throw new ConflictException("An account with this email already exists.");

        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User", cancellationToken)
            ?? throw new InvalidOperationException("The User role has not been seeded.");

        var user = new AppUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            RegistrationDate = DateTime.UtcNow,
            Role = defaultRole
        };
        user.PasswordHash = _passwordHasher.Hash(user, request.Password);

        _context.AppUsers.Add(user);
        var response = AuthSession.Start(_context, _tokenService, user);
        await _context.SaveChangesAsync(cancellationToken);

        return response;
    }
}
