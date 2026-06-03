using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StampService.Application.Access;
using StampService.Application.Audit;
using StampService.Application.Auth;
using StampService.Application.Brands;
using StampService.Application.CoinProducts;
using StampService.Application.Coins;
using StampService.Application.CustomerNotifications;
using StampService.Application.Demo;
using StampService.Application.Ledger;
using StampService.Application.Metrics;
using StampService.Application.Users;
using StampService.Infrastructure;
using StampService.Infrastructure.Repositories;
using StampService.Infrastructure.Seeding;
using StampService.Infrastructure.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBrandMembershipRepository, BrandMembershipRepository>();
        services.AddScoped<ILoyaltyMetricRepository, LoyaltyMetricRepository>();
        services.AddScoped<IMetricBalanceRepository, MetricBalanceRepository>();
        services.AddScoped<IStampTransactionRepository, StampTransactionRepository>();
        services.AddScoped<ICoinWalletRepository, CoinWalletRepository>();
        services.AddScoped<ICoinTransactionRepository, CoinTransactionRepository>();
        services.AddScoped<ICoinProductRepository, CoinProductRepository>();
        services.AddScoped<IRedemptionCodeRepository, RedemptionCodeRepository>();
        services.AddScoped<ICustomerDigestStateRepository, CustomerDigestStateRepository>();
        services.AddScoped<ICustomerRewardDigestRepository, CustomerRewardDigestRepository>();
        services.AddScoped<IRewardDigestSettingsRepository, RewardDigestSettingsRepository>();
        services.AddScoped<IBusinessAuditLogRepository, BusinessAuditLogRepository>();
        services.AddScoped<IBusinessAuditSink, BusinessAuditSink>();
        services.AddScoped<IDemoDatabaseResetService, DemoDatabaseResetService>();
        services.AddScoped<IPhoneAuthCodeRepository, PhoneAuthCodeRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<HttpClient>();
        services.AddScoped<IPhoneAuthCodeSender, TelegramAdminPhoneAuthCodeSender>();
        services.AddScoped<StampService.Application.CustomerNotifications.ICustomerNotificationService, TelegramCustomerNotificationService>();
        services.AddScoped<ILedgerOperationLock, PostgresLedgerOperationLock>();
        services.AddScoped<ITelegramLinkSessionProtector, CompactTelegramLinkSessionProtector>();

        return services;
    }
}
