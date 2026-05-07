using StackExchange.Redis;
using Testcontainers.Redis;

namespace TelegramBotFlow.IntegrationTests.Infrastructure;

/// <summary>
/// Поднимает Redis-контейнер один раз на всю коллекцию тестов.
/// </summary>
public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:7-alpine")
        .Build();

    public IConnectionMultiplexer Connection { get; private set; } = null!;

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Connection = await ConnectionMultiplexer.ConnectAsync($"{ConnectionString},allowAdmin=true");
    }

    public async Task DisposeAsync()
    {
        Connection.Dispose();
        await _container.StopAsync();
        await _container.DisposeAsync();
    }
}