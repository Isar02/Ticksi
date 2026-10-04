using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ticksi.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        try
        {
            var context = provider.GetRequiredService<AppDbContext>();
            await context.Database.MigrateAsync(cancellationToken);
            await DbSeeder.SeedAsync(context, provider.GetRequiredService<IConfiguration>(), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogCritical(exception, "Database initialization failed; the application will not start.");
            throw;
        }
    }
}
