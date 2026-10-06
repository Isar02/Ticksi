using MediatR;
using Ticksi.Application.Interfaces;

namespace Ticksi.Application.Features.Profile.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, ProfileDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;

    public UpdateProfileCommandHandler(IAppDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<ProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await ProfileOwner.LoadAsync(_context, _currentUser, cancellationToken);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        await _context.SaveChangesAsync(cancellationToken);

        return ProfileDto.From(user);
    }
}
