using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using Ticksi.Application.Common.Behaviors;

namespace Ticksi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        QuestPDF.Settings.License = LicenseType.Community;

        return services;
    }
}
