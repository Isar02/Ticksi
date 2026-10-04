using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IAppDbContext _context;
    private readonly IJwtTokenService _tokenService;

    public LoginCommandHandler(IAppDbContext context, IJwtTokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.AppUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        return new AuthResponseDto
        {
            Token = _tokenService.CreateAccessToken(user),
            Email = user.Email,
            PublicId = user.PublicId.ToString(),
            FirstName = user.FirstName
        };
    }

    private static bool VerifyPassword(string password, string passwordHash)
    {
        var hash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
        return hash == passwordHash;
    }
}
