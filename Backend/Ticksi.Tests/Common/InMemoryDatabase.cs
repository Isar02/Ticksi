using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ticksi.Tests.Common;

public sealed class InMemoryDatabase
{
    // One shared root keeps EF to a single internal service provider; the unique name isolates each test.
    private static readonly InMemoryDatabaseRoot Root = new();

    private readonly string _name = Guid.NewGuid().ToString();

    public AppDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_name, Root)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(interceptors)
            .Options;

        var context = new InMemoryAppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class InMemoryAppDbContext(DbContextOptions options) : AppDbContext(options)
    {
        public override Task<IDbContextTransaction> BeginUserAdministrationAsync(CancellationToken cancellationToken = default) =>
            BeginTransactionAsync(cancellationToken);
    }
}
