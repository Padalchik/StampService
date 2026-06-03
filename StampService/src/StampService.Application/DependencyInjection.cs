using Microsoft.Extensions.DependencyInjection;
using StampService.Application.Abstractions;
using StampService.Application.Access;
using StampService.Application.Administration;
using StampService.Application.Audit;
using StampService.Application.Auth;
using StampService.Application.Coins;
using StampService.Application.CustomerNotifications;
using StampService.Application.Ledger;
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

        // Keep command and query selectors explicit so Scrutor does not mix registrations.
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
        services.AddScoped<IBusinessAuditContext, DefaultBusinessAuditContext>();
        services.AddScoped<IBusinessAuditSink>(_ => NoopBusinessAuditSink.Instance);
        services.AddScoped<IBrandAccessService, BrandAccessService>();
        services.AddScoped<IBrandMembershipService, BrandMembershipService>();
        services.AddScoped<ICoinLedgerService, CoinLedgerService>();
        services.AddScoped<IMetricLedgerService, MetricLedgerService>();
        services.AddScoped<IRedeemMetricValidationService, RedeemMetricValidationService>();
        services.AddScoped<ITelegramValidationService, TelegramValidationService>();
        services.AddScoped<IUserDisplayNameGenerator, CuteUserDisplayNameGenerator>();
        services.AddScoped<IPhoneAuthCodeGenerator, PhoneAuthCodeGenerator>();
        services.AddScoped<IPhoneAuthCodeService, PhoneAuthCodeService>();
        services.AddScoped<IPhoneAccountService, PhoneAccountService>();
        services.AddScoped<IRedemptionCodeGenerator, RedemptionCodeGenerator>();
        services.AddScoped<ICustomerNotificationService>(_ => NullCustomerNotificationService.Instance);
        services.AddScoped<ILedgerOperationLock>(_ => NoopLedgerOperationLock.Instance);
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
