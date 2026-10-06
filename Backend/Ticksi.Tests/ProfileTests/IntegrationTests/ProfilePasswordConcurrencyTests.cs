using System.Data.Common;
using FluentValidation;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.Features.Profile.Commands.ChangePassword;
using Ticksi.Application.Interfaces;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.ProfileTests.IntegrationTests;

public class ProfilePasswordConcurrencyTests(TicksiApiFactory factory) : IClassFixture<TicksiApiFactory>
{
    [Fact]
    public async Task ChangePassword_OverlappingRequests_RejectsTheOldPasswordAndKeepsTheFirstSession()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var role = await database.Roles.SingleAsync(r => r.Name == Role.Names.User);
        var user = new AppUser
        {
            FirstName = "Ana", LastName = "Kovac", Phone = "061-123-456",
            Email = $"password.{Guid.NewGuid():N}@ticksi.com", Role = role
        };
        user.PasswordHash = hasher.Hash(user, "Secret123");
        database.AppUsers.Add(user);
        await database.SaveChangesAsync();
        var originalTokens = tokens.IssueTokens(user);
        database.RefreshTokens.Add(new RefreshToken
        {
            AppUserId = user.Id, TokenHash = originalTokens.RefreshTokenHash,
            ExpiresAtUtc = originalTokens.RefreshTokenExpiresAtUtc
        });
        await database.SaveChangesAsync();

        var connection = database.Database.GetConnectionString()!;
        var paused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondAttempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pauseFirst = new BeforeSaveInterceptor(1, async cancellationToken =>
        {
            paused.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        });
        await using var firstContext = Context(connection, pauseFirst);
        await using var secondContext = Context(connection, new PasswordLockAttemptInterceptor(secondAttempt));
        var caller = new FakeCurrentUser(user.PublicId);
        var first = new ChangePasswordCommandHandler(firstContext, caller, tokens, hasher, clock);
        var second = new ChangePasswordCommandHandler(secondContext, caller, tokens, hasher, clock);
        var firstRequest = first.Handle(
            new ChangePasswordCommand { CurrentPassword = "Secret123", NewPassword = "First456" }, CancellationToken.None);
        Task<Exception?>? secondRequest = null;
        try
        {
            await paused.Task.WaitAsync(TimeSpan.FromSeconds(15));
            secondRequest = Record.ExceptionAsync(() => second.Handle(
                new ChangePasswordCommand { CurrentPassword = "Secret123", NewPassword = "Second789" }, CancellationToken.None));
            await secondAttempt.Task.WaitAsync(TimeSpan.FromSeconds(15));
            Assert.False(secondRequest.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
            await firstRequest;
        }

        var error = Assert.IsType<ValidationException>(await secondRequest!);
        Assert.Equal(nameof(ChangePasswordCommand.CurrentPassword), Assert.Single(error.Errors).PropertyName);
        var session = await firstRequest;
        await using var verify = Context(connection);
        var savedUser = await verify.AppUsers.SingleAsync(u => u.Id == user.Id);
        Assert.NotEqual(PasswordCheck.Failed, hasher.Verify(savedUser, "First456"));
        Assert.Equal(PasswordCheck.Failed, hasher.Verify(savedUser, "Second789"));
        var active = Assert.Single(await verify.RefreshTokens.Where(t => t.AppUserId == user.Id && t.RevokedAtUtc == null).ToListAsync());
        Assert.Equal(tokens.HashRefreshToken(session.RefreshToken), active.TokenHash);
    }

    private static AppDbContext Context(string connection, params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).AddInterceptors(interceptors).Options);

    private sealed class PasswordLockAttemptInterceptor(TaskCompletionSource attempted) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("UPDLOCK", StringComparison.Ordinal))
                attempted.TrySetResult();
            return ValueTask.FromResult(result);
        }
    }
}
