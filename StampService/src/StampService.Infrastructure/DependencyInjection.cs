using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StampService.Application.Access;
using StampService.Application.Auth;
using StampService.Application.Brands;
using StampService.Application.Metrics;
using StampService.Application.Users;
using StampService.Infrastructure;
using StampService.Infrastructure.Repositories;
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
        services.AddScoped<IRedemptionCodeRepository, RedemptionCodeRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
