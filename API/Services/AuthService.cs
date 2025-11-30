using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto loginDto)
    {
        var user = await _context.AppUsers
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null)
        {
            var errorMessage = _configuration["Messages:Auth:InvalidCredentials"] ?? "Invalid credentials.";
            return ServiceResult<AuthResponseDto>.Failure(errorMessage);
        }

        if (!VerifyPassword(loginDto.Password, user.PasswordHash))
        {
            var errorMessage = _configuration["Messages:Auth:InvalidCredentials"] ?? "Invalid credentials.";
            return ServiceResult<AuthResponseDto>.Failure(errorMessage);
        }

        var token = GenerateJwtToken(user);

        var response = new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            PublicId = user.PublicId,
            FirstName = user.FirstName
        };

        return ServiceResult<AuthResponseDto>.Success(response);
    }

    public async Task<ServiceResult<AuthResponseDto>> RegisterAsync(RegisterDto registerDto)
    {
        // Check if user already exists
        var existingUser = await _context.AppUsers
            .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

        if (existingUser != null)
        {
            var errorMessage = _configuration["Messages:Auth:EmailExists"] ?? "Email already exists.";
            return ServiceResult<AuthResponseDto>.Failure(errorMessage);
        }

        // Get default role (assuming role with Name = "User")
        var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (defaultRole == null)
        {
            // If no User role exists, get the first available role
            defaultRole = await _context.Roles.FirstOrDefaultAsync();
            if (defaultRole == null)
            {
                var errorMessage = _configuration["Messages:Auth:RegistrationFailed"] ?? "Registration failed.";
                return ServiceResult<AuthResponseDto>.Failure(errorMessage);
            }
        }

        var defaultStatus = _configuration["Seeding:DefaultStatus"] ?? "Active";
        
        var user = new AppUser
        {
            FirstName = registerDto.FirstName,
            LastName = registerDto.LastName,
            Email = registerDto.Email,
            PasswordHash = HashPassword(registerDto.Password),
            Phone = registerDto.Phone,
            RegistrationDate = DateTime.UtcNow,
            Status = defaultStatus,
            RoleId = defaultRole.Id
        };

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);

        var response = new AuthResponseDto
        {
            Token = token,
            Email = user.Email,
            PublicId = user.PublicId,
            FirstName = user.FirstName
        };

        return ServiceResult<AuthResponseDto>.Success(response);
    }

    public string GenerateJwtToken(AppUser user)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var jwtIssuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        var jwtAudience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");
        var jwtExpirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.PublicId),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim("RoleId", user.RoleId.ToString())
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

