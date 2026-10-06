using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Ticksi.Tests.Common;

public sealed class TicksiApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string SettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.IntegrationTests.json");

    private readonly string _connectionString = IsolatedConnectionString();

    // Uploaded files go to a folder of their own instead of the API's wwwroot.
    public string WebRoot { get; } = Directory.CreateDirectory(
        Path.Combine(Path.GetTempPath(), $"ticksi-tests-{Guid.NewGuid():N}")).FullName;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTests");
        builder.UseWebRoot(WebRoot);
        builder.ConfigureAppConfiguration(config => config
            .AddJsonFile(SettingsPath, optional: false)
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString
            }));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options;
        await using var context = new AppDbContext(options);
        await context.Database.EnsureDeletedAsync();

        Directory.Delete(WebRoot, recursive: true);
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    // Every factory migrates its own database, so test runs never touch each other or the development data.
    private static string IsolatedConnectionString()
    {
        var settings = new ConfigurationBuilder().AddJsonFile(SettingsPath, optional: false).Build();
        var connection = new SqlConnectionStringBuilder(settings.GetConnectionString("DefaultConnection"));
        connection.InitialCatalog = $"{connection.InitialCatalog}_{Guid.NewGuid():N}";
        return connection.ConnectionString;
    }
}
