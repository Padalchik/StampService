using Microsoft.Extensions.DependencyInjection;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Administration;
using StampService.Application.Auth;
using StampService.Application.Metrics;
using StampService.Application.Metrics.Commands.RedeemMetric;
using StampService.Application.Services;
using StampService.Application.Users;

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
        services.AddScoped<IAdminAccessService, AdminAccessService>();
        services.AddScoped<IBrandAccessService, BrandAccessService>();
        services.AddScoped<IBrandMembershipService, BrandMembershipService>();
        services.AddScoped<IMetricLedgerService, MetricLedgerService>();
        services.AddScoped<IRedeemMetricValidationService, RedeemMetricValidationService>();
        services.AddScoped<ITelegramValidationService, TelegramValidationService>();
        services.AddScoped<ICustomerCodeGenerator, CustomerCodeGenerator>();
        services.AddScoped<IRedemptionCodeGenerator, RedemptionCodeGenerator>();
        services.AddScoped<IRecipientResolver, RecipientResolver>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
