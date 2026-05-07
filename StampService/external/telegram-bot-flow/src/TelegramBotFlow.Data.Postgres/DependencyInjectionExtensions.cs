using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Users;

namespace TelegramBotFlow.Core.Data;

public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Registers BotDbContext with default BotUser model, EfBotUserStore, and UserTrackingMiddleware.
    /// Connection string is read from Configuration["ConnectionStrings:Database"].
    /// </summary>
    public static IServiceCollection AddBotCoreData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("Database")
                                  ?? throw new InvalidOperationException("Connection string 'Database' not found.");

        services.AddDbContext<BotDbContext>(options =>
           options.UseNpgsql(connectionString));

        // Register base type so EfBotUserStore<BotUser> can resolve BotDbContext<BotUser>
        services.AddScoped<BotDbContext<BotUser>>(sp => sp.GetRequiredService<BotDbContext>());

        services.AddScoped<IBotUserStore<BotUser>, EfBotUserStore<BotUser>>();
        services.AddTransient<UserTrackingMiddleware<BotUser>>();

        return services;
    }

    /// <summary>
    /// Registers a custom BotDbContext with a custom user type, EfBotUserStore, and UserTrackingMiddleware.
    /// Use when you need to extend BotUser with additional properties.
    /// <code>
    /// services.AddBotCoreData&lt;AppUser, AppDbContext&gt;(configuration);
    /// </code>
    /// </summary>
    public static IServiceCollection AddBotCoreData<TUser, TContext>(
        this IServiceCollection services,
        IConfiguration configuration)
        where TUser : BotUser, new()
        where TContext : BotDbContext<TUser>
    {
        string connectionString = configuration.GetConnectionString("Database")
                                  ?? throw new InvalidOperationException("Connection string 'Database' not found.");

        services.AddDbContext<TContext>(options =>
           options.UseNpgsql(connectionString));

        // Register base type so modules can resolve BotDbContext<TUser>
        services.AddScoped<BotDbContext<TUser>>(sp => sp.GetRequiredService<TContext>());

        services.AddScoped<IBotUserStore<TUser>, EfBotUserStore<TUser>>();
        services.AddTransient<UserTrackingMiddleware<TUser>>();

        return services;
    }
}
