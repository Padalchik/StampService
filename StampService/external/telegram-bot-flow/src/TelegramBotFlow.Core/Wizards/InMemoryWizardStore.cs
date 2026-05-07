using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TelegramBotFlow.Core.Hosting;

namespace TelegramBotFlow.Core.Wizards;

/// <summary>
/// In-memory wizard state store backed by IMemoryCache with automatic expiration.
/// Suitable for development. In production consider Redis or a database.
/// </summary>
internal sealed class InMemoryWizardStore : IWizardStore
{
    private readonly IMemoryCache _cache;
    private readonly int _defaultTtlMinutes;

    public InMemoryWizardStore(IMemoryCache cache, IOptions<BotConfiguration> config)
    {
        _cache = cache;
        _defaultTtlMinutes = config.Value.WizardDefaultTtlMinutes;
    }

    public Task<WizardStorageState?> GetAsync(long userId, string wizardId,
        CancellationToken cancellationToken = default)
    {
        _cache.TryGetValue(FormatKey(userId, wizardId), out WizardStorageState? state);
        return Task.FromResult(state);
    }

    public Task SaveAsync(long userId, string wizardId, WizardStorageState state,
        CancellationToken cancellationToken = default)
    {
        var options = new MemoryCacheEntryOptions();
        if (state.ExpiresAt.HasValue)
            options.SetAbsoluteExpiration(state.ExpiresAt.Value);
        else
            options.SetSlidingExpiration(TimeSpan.FromMinutes(_defaultTtlMinutes));

        _cache.Set(FormatKey(userId, wizardId), state, options);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(long userId, string wizardId, CancellationToken cancellationToken = default)
    {
        _cache.Remove(FormatKey(userId, wizardId));
        return Task.CompletedTask;
    }

    private static string FormatKey(long userId, string wizardId) => $"wizard:{userId}:{wizardId}";
}
