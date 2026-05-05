using Microsoft.Extensions.DependencyInjection;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Auth;
using StampService.Application.Services;

namespace StampService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // DirectoryService uses a different Scrutor chain here; keep each selector explicit.
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(c => c.AssignableTo(typeof(ICommandHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(c => c.AssignableTo(typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IBrandAccessService, BrandAccessService>();
        services.AddScoped<IBrandMembershipService, BrandMembershipService>();
        services.AddScoped<ITelegramValidationService, TelegramValidationService>();

        return services;
    }
}
