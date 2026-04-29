using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StampService.Application.Auth;
using StampService.Application.Brands;
using StampService.Infrastructure;
using StampService.Infrastructure.Repositories;
using StampService.Infrastructure.Services;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")!;

        services.AddScoped<AppDbContext>(_ =>
            new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString)
                .Options));

        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
