using Ticksi.Application.DTOs;
using Ticksi.Domain.Entities;
using Ticksi.Application.Models;

namespace Ticksi.Application.Interfaces;

public interface IAuthService
{
    Task<ServiceResult<AuthResponseDto>> LoginAsync(LoginDto loginDto);
    Task<ServiceResult<AuthResponseDto>> RegisterAsync(RegisterDto registerDto);
    string GenerateJwtToken(AppUser user);
}

