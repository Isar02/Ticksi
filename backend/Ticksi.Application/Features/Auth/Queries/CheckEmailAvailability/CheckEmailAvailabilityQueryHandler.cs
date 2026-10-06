using MediatR;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Auth.Queries.CheckEmailAvailability;

public class CheckEmailAvailabilityQueryHandler : IRequestHandler<CheckEmailAvailabilityQuery, EmailAvailabilityDto>
{
    private readonly IAppDbContext _context;

    public CheckEmailAvailabilityQueryHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<EmailAvailabilityDto> Handle(CheckEmailAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var taken = await _context.AppUsers.AnyAsync(u => u.Email == request.Email, cancellationToken);

        return new EmailAvailabilityDto(!taken);
    }
}
