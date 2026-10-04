using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ticksi.Tests.Common;

public sealed class InMemoryDatabase
{
    private readonly string _name = Guid.NewGuid().ToString();
    private readonly InMemoryDatabaseRoot _root = new();

    public AppDbContext CreateContext(params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(_name, _root)
            .AddInterceptors(interceptors)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
