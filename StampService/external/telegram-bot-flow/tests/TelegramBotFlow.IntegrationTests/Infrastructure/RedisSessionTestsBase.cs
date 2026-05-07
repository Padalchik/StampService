using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TelegramBotFlow.Core.Sessions.Redis;

namespace TelegramBotFlow.IntegrationTests.Infrastructure;

[Collection(nameof(RedisIntegrationTests))]
public abstract class RedisSessionTestsBase : IAsyncLifetime
{
    private readonly RedisFixture _fixture;

    protected RedisSessionTestsBase(RedisFixture fixture)
    {
        _fixture = fixture;
    }

    protected IConnectionMultiplexer Redis => _fixture.Connection;

    protected IDatabase Db => Redis.GetDatabase();

    protected RedisSessionStore CreateStore(int? sessionTtlMinutes = null)
    {
        IOptions<RedisSessionOptions> options = Options.Create(new RedisSessionOptions
        {
            SessionTtlMinutes = sessionTtlMinutes
        });

        return new RedisSessionStore(Redis, options);
    }

    public async Task InitializeAsync()
    {
        IServer server = Redis.GetServer(Redis.GetEndPoints().First());
        await server.FlushDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
}