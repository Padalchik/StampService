using Microsoft.Extensions.Options;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.Core.Sessions;

/// <summary>
/// In-memory реализация провайдера блокировок сессий с использованием паттерна Striped Locking.
/// Подходит только для single-instance развертывания бота.
/// </summary>
internal sealed class InMemorySessionLockProvider : ISessionLockProvider
{
    private const int STRIPE_COUNT = 1024;
    private readonly SemaphoreSlim[] _locks;
    private readonly TimeSpan _lockTimeout;

    public InMemorySessionLockProvider(IOptions<BotConfiguration> config)
        : this(TimeSpan.FromSeconds(config.Value.SessionLockTimeoutSeconds))
    {
    }

    /// <summary>Test-only constructor for explicit timeout override.</summary>
    internal InMemorySessionLockProvider(TimeSpan? lockTimeout = null)
    {
        _lockTimeout = lockTimeout ?? TimeSpan.FromSeconds(10);
        _locks = new SemaphoreSlim[STRIPE_COUNT];
        for (int i = 0; i < STRIPE_COUNT; i++)
            _locks[i] = new SemaphoreSlim(1, 1);
    }

    public async Task<IDisposable> AcquireLockAsync(long userId, CancellationToken cancellationToken = default)
    {
        // Вычисляем индекс корзины (stripe) на основе userId — равномерное распределение без коллизий GetHashCode
        int index = (int)((ulong)userId % STRIPE_COUNT);
        SemaphoreSlim semaphore = _locks[index];

        bool acquired = await semaphore.WaitAsync(_lockTimeout, cancellationToken);
        if (!acquired)
        {
            throw new TimeoutException(
                $"Failed to acquire session lock for user {userId} within {_lockTimeout.TotalSeconds} seconds.");
        }

        return new LockReleaser(semaphore);
    }

    private sealed class LockReleaser : IDisposable
    {
        private readonly SemaphoreSlim _semaphore;

        public LockReleaser(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public void Dispose()
        {
            _semaphore.Release();
        }
    }
}