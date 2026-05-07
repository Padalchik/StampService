using FluentAssertions;
using Microsoft.Extensions.Options;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Sessions;

public sealed class ConcurrentSessionTests
{
    [Fact]
    public async Task ParallelAccess_SameUser_WithLock_NoCorruption()
    {
        var store = new InMemorySessionStore();
        var config = Options.Create(new BotConfiguration { Token = "test", SessionLockTimeoutSeconds = 5 });
        var lockProvider = new InMemorySessionLockProvider(config);

        var tasks = Enumerable.Range(0, 50).Select(async i =>
        {
            using var lockHandle = await lockProvider.AcquireLockAsync(1);
            var session = await store.GetOrCreateAsync(1);
            var current = session.Data.GetInt("counter") ?? 0;
            session.Data.Set("counter", current + 1);
            await store.SaveAsync(session);
        });

        await Task.WhenAll(tasks);

        var finalSession = await store.GetOrCreateAsync(1);
        finalSession.Data.GetInt("counter").Should().Be(50);
    }

    [Fact]
    public async Task ParallelAccess_DifferentUsers_NoContention()
    {
        var store = new InMemorySessionStore();
        var config = Options.Create(new BotConfiguration { Token = "test", SessionLockTimeoutSeconds = 5 });
        var lockProvider = new InMemorySessionLockProvider(config);

        var tasks = Enumerable.Range(1, 100).Select(async userId =>
        {
            using var lockHandle = await lockProvider.AcquireLockAsync(userId);
            var session = await store.GetOrCreateAsync(userId);
            session.Data.Set("id", userId);
            await store.SaveAsync(session);
        });

        await Task.WhenAll(tasks);

        for (int i = 1; i <= 100; i++)
        {
            var s = await store.GetOrCreateAsync(i);
            s.Data.GetInt("id").Should().Be(i);
        }
    }
}
