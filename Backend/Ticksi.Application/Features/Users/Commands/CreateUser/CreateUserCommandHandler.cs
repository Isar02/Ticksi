using MediatR;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDto>
{
    private readonly IAppDbContext _context;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;

    public CreateUserCommandHandler(
        IAppDbContext context,
        ICurrentUser currentUser,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider)
    {
        _context = context;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
    }

    public async Task<UserDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        await UserAdministrator.ResolveAsync(_context, _currentUser, cancellationToken);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var user = new AppUser { RegistrationDate = now };
        await UserWriter.ApplyAsync(_context, user, request, cancellationToken);
        user.PasswordHash = _passwordHasher.Hash(user, request.Password);

        _context.AppUsers.Add(user);
        await UserWriter.SaveAsync(_context, user, wasActive: false, now, cancellationToken);

        return UserDto.From(user);
    }
}
