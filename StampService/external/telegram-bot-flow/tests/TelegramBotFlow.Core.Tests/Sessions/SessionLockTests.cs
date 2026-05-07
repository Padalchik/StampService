using FluentAssertions;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Sessions;

public sealed class SessionLockTests
{
    [Fact]
    public async Task AcquireLockAsync_Timeout_ThrowsTimeoutException()
    {
        // Arrange — extremely short timeout so the second acquire times out immediately
        var provider = new InMemorySessionLockProvider(TimeSpan.FromMilliseconds(50));
        long userId = 42;

        // Acquire and hold the lock without releasing
        IDisposable firstLock = await provider.AcquireLockAsync(userId);

        // Act — second acquire on the same stripe should time out
        Func<Task> act = () => provider.AcquireLockAsync(userId);

        // Assert
        await act.Should().ThrowAsync<TimeoutException>();

        firstLock.Dispose();
    }

    [Fact]
    public async Task AcquireLockAsync_DifferentUserIds_NoContention()
    {
        // Arrange — use user IDs that map to different stripes
        // Stripe index = userId % 1024, so 1 and 2 map to different stripes
        var provider = new InMemorySessionLockProvider(TimeSpan.FromSeconds(5));
        long userId1 = 1;
        long userId2 = 2;

        // Act — acquire both locks concurrently; neither should block the other
        Task<IDisposable> task1 = provider.AcquireLockAsync(userId1);
        Task<IDisposable> task2 = provider.AcquireLockAsync(userId2);

        IDisposable[] locks = await Task.WhenAll(task1, task2);

        // Assert — both completed successfully (no exception, no deadlock)
        locks.Should().HaveCount(2);
        locks[0].Should().NotBeNull();
        locks[1].Should().NotBeNull();

        locks[0].Dispose();
        locks[1].Dispose();
    }

    [Fact]
    public async Task AcquireLockAsync_AfterRelease_CanAcquireAgain()
    {
        // Arrange
        var provider = new InMemorySessionLockProvider(TimeSpan.FromMilliseconds(200));
        long userId = 99;

        // Act — acquire, release, then re-acquire
        IDisposable firstLock = await provider.AcquireLockAsync(userId);
        firstLock.Dispose();

        Func<Task> act = async () =>
        {
            IDisposable secondLock = await provider.AcquireLockAsync(userId);
            secondLock.Dispose();
        };

        // Assert — no exception on second acquire after release
        await act.Should().NotThrowAsync();
    }
}
