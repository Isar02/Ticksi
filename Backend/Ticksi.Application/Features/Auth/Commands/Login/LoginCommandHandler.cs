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
    private readonly IPasswordHasher _passwordHasher;

    public LoginCommandHandler(IAppDbContext context, IJwtTokenService tokenService, IPasswordHasher passwordHasher)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.AppUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        var passwordCheck = user is null ? PasswordCheck.Failed : _passwordHasher.Verify(user, request.Password);
        if (user is null || passwordCheck == PasswordCheck.Failed)
            throw new UnauthorizedException("Invalid email or password.");

        if (passwordCheck == PasswordCheck.ValidNeedsRehash)
        {
            user.PasswordHash = _passwordHasher.Hash(user, request.Password);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new AuthResponseDto
        {
            Token = _tokenService.CreateAccessToken(user),
            Email = user.Email,
            PublicId = user.PublicId.ToString(),
            FirstName = user.FirstName
        };
    }
}
