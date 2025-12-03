using API.DTOs;
using Ticksi.Domain.Entities;
using API.Models;

namespace API.Services;

public interface IAuthService
{
    Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto loginDto);
    Task<ServiceResult<AuthResponseDto>> RegisterAsync(RegisterDto registerDto);
    string GenerateJwtToken(AppUser user);
}

