using System.Security.Cryptography;
using System.Text;
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

    public RegisterCommandHandler(IAppDbContext context, IJwtTokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
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
            PasswordHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(request.Password))),
            Phone = request.Phone,
            RegistrationDate = DateTime.UtcNow,
            Role = defaultRole
        };

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResponseDto
        {
            Token = _tokenService.CreateAccessToken(user),
            Email = user.Email,
            PublicId = user.PublicId.ToString(),
            FirstName = user.FirstName
        };
    }
}
