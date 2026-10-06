using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ticksi.Application.Interfaces;
using Ticksi.Application.Options;
using Ticksi.Infrastructure.Data;
using Ticksi.Infrastructure.Data.Seeders;
using Ticksi.Infrastructure.Options;
using Ticksi.Infrastructure.Payments;
using Ticksi.Infrastructure.QrCodes;
using Ticksi.Infrastructure.Reports;
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

        services.AddOptions<ConnectionStringsOptions>()
            .Bind(configuration.GetSection(ConnectionStringsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SeedingOptions>()
            .Bind(configuration.GetSection(SeedingOptions.SectionName))
            .Validate(seeding => !seeding.DemoData || !string.IsNullOrWhiteSpace(seeding.DemoPassword),
                "Seeding:DemoPassword is required when demo data is enabled.")
            .ValidateOnStart();

        // Without keys the app still starts; only the payment calls fail until they are set in user secrets.
        services.AddOptions<StripeOptions>()
            .Bind(configuration.GetSection(StripeOptions.SectionName));

        services.AddDbContext<AppDbContext>((provider, options) =>
            options.UseSqlServer(provider.GetRequiredService<IOptions<ConnectionStringsOptions>>().Value.DefaultConnection));
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddScoped<DemoDataSeeder>();

        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IReportRenderer, QuestPdfReportRenderer>();
        services.AddSingleton<IPaymentGateway, StripePaymentGateway>();
        services.AddSingleton<IQrCodeGenerator, QrCoderGenerator>();

        return services;
    }
}
