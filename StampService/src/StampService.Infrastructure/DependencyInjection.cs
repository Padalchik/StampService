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
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        }, ServiceLifetime.Scoped);

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
        services.AddScoped<IPhoneAuthSmsSettingsRepository, PhoneAuthSmsSettingsRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<HttpClient>();
        services.Configure<SmsAeroOptions>(options =>
        {
            options.Login = configuration["SmsAero:Login"];
            options.ApiKey = configuration["SmsAero:ApiKey"];
            options.SendAuthCodes = !bool.TryParse(configuration["SmsAero:SendAuthCodes"], out var sendAuthCodes)
                || sendAuthCodes;
        });
        services.AddScoped<TelegramAdminPhoneAuthCodeSender>();
        services.AddScoped<SmsAeroPhoneAuthCodeSender>();
        services.AddScoped<IPhoneAuthCodeSender>(provider => new CompositePhoneAuthCodeSender(
            new IPhoneAuthCodeSender[]
            {
                provider.GetRequiredService<TelegramAdminPhoneAuthCodeSender>(),
                provider.GetRequiredService<SmsAeroPhoneAuthCodeSender>()
            }));
        services.AddScoped<StampService.Application.CustomerNotifications.ICustomerNotificationService, TelegramCustomerNotificationService>();
        services.AddScoped<ILedgerOperationLock, PostgresLedgerOperationLock>();
        services.AddScoped<ITelegramLinkSessionProtector, CompactTelegramLinkSessionProtector>();

        return services;
    }
}
