namespace Ticksi.Application.DTOs;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAtUtc { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
}
