using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Ticksi.Application.Features.Auth;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;
using Ticksi.Domain.Enums;

namespace Ticksi.Application.Features.Users.Commands;

internal static class UserWriter
{
    private const string EmailTaken = "An account with this email already exists.";

    public static async Task ApplyAsync(IAppDbContext context, AppUser user, UserInput input, CancellationToken cancellationToken)
    {
        var role = await context.Roles.FirstOrDefaultAsync(r => r.PublicId == input.RoleId, cancellationToken)
            ?? throw FieldError(nameof(UserInput.RoleId), "Choose one of the listed roles.");

        var email = input.Email.Trim();
        if (await EmailTakenAsync(context, email, user.Id, cancellationToken))
            throw FieldError(nameof(UserInput.Email), EmailTaken);

        user.FirstName = input.FirstName.Trim();
        user.LastName = input.LastName.Trim();
        user.Email = email;
        user.Phone = input.Phone.Trim();
        user.Role = role;
        user.IsActive = input.IsActive;
    }

    // Deactivation also ends every session of the account, so it cannot be refreshed into a new one.
    public static async Task SaveAsync(
        IAppDbContext context,
        AppUser user,
        bool wasActive,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // A parallel request may have taken the email between the check and the save.
            if (!await EmailTakenAsync(context, user.Email, user.Id, cancellationToken))
                throw;

            throw FieldError(nameof(UserInput.Email), EmailTaken);
        }

        if (wasActive && !user.IsActive)
            await UserSessions.EndAllAsync(context, user.Id, RefreshTokenRevocation.AccountDeactivated, nowUtc, cancellationToken);
    }

    private static Task<bool> EmailTakenAsync(IAppDbContext context, string email, int exceptUserId, CancellationToken cancellationToken) =>
        context.AppUsers.AnyAsync(u => u.Email == email && u.Id != exceptUserId, cancellationToken);

    private static ValidationException FieldError(string property, string message) =>
        new([new ValidationFailure(property, message)]);
}
