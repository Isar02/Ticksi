using MediatR;

namespace Ticksi.Application.Features.Auth.Queries.CheckEmailAvailability;

public record CheckEmailAvailabilityQuery : IRequest<EmailAvailabilityDto>
{
    public CheckEmailAvailabilityQuery(string? email)
    {
        Email = email?.Trim() ?? string.Empty;
    }

    public string Email { get; }
}
