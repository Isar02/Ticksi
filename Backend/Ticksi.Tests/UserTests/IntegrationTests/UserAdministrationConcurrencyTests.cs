using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.Common.Exceptions;
using Ticksi.Application.Features.Users.Commands.DeleteUser;
using Ticksi.Application.Features.Users.Commands.SetUserActive;
using Ticksi.Application.Features.Users.Commands.UpdateUser;
using Ticksi.Tests.Common;

namespace Ticksi.Tests.UserTests.IntegrationTests;

public class UserAdministrationConcurrencyTests(TicksiApiFactory factory) : IClassFixture<TicksiApiFactory>
{
    [Theory]
    [InlineData("deactivate")]
    [InlineData("demote")]
    [InlineData("delete")]
    public async Task MutualChanges_KeepOneAdministratorAndRejectTheOtherRequest(string operation)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var role = await database.Roles.SingleAsync(r => r.Name == Role.Names.Admin);
        var first = NewAdmin(role);
        var second = NewAdmin(role);
        database.AppUsers.AddRange(first, second);
        await database.SaveChangesAsync();
        var connection = database.Database.GetConnectionString()!;
        var paused = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondAttempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstPause = new BeforeSaveInterceptor(1, async cancellationToken =>
        {
            paused.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(15), cancellationToken);
        });
        var secondSave = new BeforeSaveInterceptor(1, _ =>
        {
            secondAttempt.TrySetResult();
            return Task.CompletedTask;
        });
        await using var firstContext = Context(connection, firstPause);
        await using var secondContext = Context(connection, secondSave, new LockAttemptInterceptor(secondAttempt));
        var firstRequest = ChangeAsync(firstContext, first, second, operation);
        Task<Exception?>? secondRequest = null;
        try
        {
            await paused.Task.WaitAsync(TimeSpan.FromSeconds(15));
            secondRequest = Record.ExceptionAsync(() => ChangeAsync(secondContext, second, first, operation));
            await secondAttempt.Task.WaitAsync(TimeSpan.FromSeconds(15));
        }
        finally
        {
            release.TrySetResult();
            await firstRequest;
        }

        var failure = await secondRequest!;
        await using var verify = Context(connection);
        var survivors = await verify.AppUsers.CountAsync(u =>
            (u.PublicId == first.PublicId || u.PublicId == second.PublicId) && u.IsActive && u.Role!.Name == Role.Names.Admin);
        Assert.Equal(1, survivors);
        if (operation == "demote")
            Assert.IsType<ForbiddenException>(failure);
        else
            Assert.IsType<UnauthorizedException>(failure);
    }

    private static AppUser NewAdmin(Role role) => new()
    {
        FirstName = "Lejla", LastName = "Begic", Email = $"admin.{Guid.NewGuid():N}@ticksi.com",
        Phone = "+387 61 123 456", PasswordHash = "hash", Role = role
    };

    private static AppDbContext Context(string connection, params IInterceptor[] interceptors) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).AddInterceptors(interceptors).Options);

    private static async Task ChangeAsync(AppDbContext context, AppUser actor, AppUser target, string operation)
    {
        var caller = new FakeCurrentUser(actor.PublicId);
        if (operation == "delete")
            await new DeleteUserCommandHandler(context, caller).Handle(new DeleteUserCommand { PublicId = target.PublicId }, CancellationToken.None);
        else if (operation == "deactivate")
            await new SetUserActiveCommandHandler(context, caller, TimeProvider.System).Handle(
                new SetUserActiveCommand { PublicId = target.PublicId, IsActive = false }, CancellationToken.None);
        else
            await new UpdateUserCommandHandler(context, caller, TimeProvider.System).Handle(new UpdateUserCommand
            {
                PublicId = target.PublicId, FirstName = target.FirstName, LastName = target.LastName,
                Email = target.Email, Phone = target.Phone, IsActive = true,
                RoleId = await context.Roles.Where(r => r.Name == Role.Names.User).Select(r => r.PublicId).SingleAsync()
            }, CancellationToken.None);
    }

    private sealed class LockAttemptInterceptor(TaskCompletionSource attempted) : DbCommandInterceptor
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
