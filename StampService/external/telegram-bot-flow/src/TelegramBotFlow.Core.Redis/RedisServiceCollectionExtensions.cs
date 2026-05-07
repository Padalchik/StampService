using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.Core.Sessions.Redis;

namespace TelegramBotFlow.Core.Redis;

/// <summary>
/// DI-регистрация Redis session store.
/// Вызывается после <c>AddTelegramBotFlow()</c> — заменяет InMemorySessionStore на Redis.
/// </summary>
public static class RedisServiceCollectionExtensions
{
    public static IServiceCollection AddRedisSessionStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RedisSessionOptions>(
           configuration.GetSection(RedisSessionOptions.SECTION_NAME));

        RedisSessionOptions options =
            configuration.GetSection(RedisSessionOptions.SECTION_NAME).Get<RedisSessionOptions>()
            ?? new RedisSessionOptions();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(options.ConnectionString));

        // Заменяем InMemorySessionStore на Redis
        services.RemoveAll<ISessionStore>();
        services.AddSingleton<ISessionStore, RedisSessionStore>();

        return services;
    }
}