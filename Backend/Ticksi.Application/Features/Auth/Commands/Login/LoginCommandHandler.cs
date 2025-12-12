using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Ticksi.Application.DTOs;
using Ticksi.Application.Interfaces;
using Ticksi.Application.Models;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Auth.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, ServiceResult<AuthResponseDto>>
    {
        private readonly IAppDbContext _context;
        private readonly IConfiguration _configuration;

        public LoginCommandHandler(IAppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<ServiceResult<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.AppUsers
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (user == null)
            {
                var errorMessage = _configuration["Messages:Auth:InvalidCredentials"] ?? "Invalid credentials.";
                return ServiceResult<AuthResponseDto>.Failure(errorMessage);
            }

            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                var errorMessage = _configuration["Messages:Auth:InvalidCredentials"] ?? "Invalid credentials.";
                return ServiceResult<AuthResponseDto>.Failure(errorMessage);
            }

            var token = GenerateJwtToken(user);

            var response = new AuthResponseDto
            {
                Token = token,
                Email = user.Email,
                PublicId = user.PublicId.ToString(),
                FirstName = user.FirstName
            };

            return ServiceResult<AuthResponseDto>.Success(response);
        }

        private string GenerateJwtToken(AppUser user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
            var jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
            var jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
            var jwtExpirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.PublicId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
                new Claim("RoleId", user.Role!.Name)
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(jwtExpirationMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            var hashOfInput = HashPassword(password);
            return hashOfInput == passwordHash;
        }
    }
}

