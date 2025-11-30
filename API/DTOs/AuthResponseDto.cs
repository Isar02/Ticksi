namespace API.DTOs;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
}

