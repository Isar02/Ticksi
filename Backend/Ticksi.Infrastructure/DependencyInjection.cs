using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ticksi.Application.Interfaces;
using Ticksi.Application.Options;
using Ticksi.Infrastructure.Data;
using Ticksi.Infrastructure.Security;
using Ticksi.Infrastructure.Services;

namespace Ticksi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<FileUploadOptions>()
            .Bind(configuration.GetSection(FileUploadOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(BuildConnectionString(configuration)));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }

    private static string BuildConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        var databaseFile = Path.Combine(solutionRoot, "db-backups", "TicksiDb.mdf");
        Directory.CreateDirectory(Path.GetDirectoryName(databaseFile)!);

        return connectionString.Replace("{DbPath}", databaseFile);
    }
}
