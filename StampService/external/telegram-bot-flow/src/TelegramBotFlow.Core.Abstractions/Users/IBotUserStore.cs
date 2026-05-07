namespace TelegramBotFlow.Core.Users;

/// <summary>
/// Persistence abstraction for bot users.
/// Implementations: EfBotUserStore (Data.Postgres), custom stores.
/// </summary>
public interface IBotUserStore<TUser> where TUser : class, IBotUser
{
    Task<TUser?> FindByTelegramIdAsync(long telegramId, CancellationToken ct = default);
    Task CreateAsync(TUser user, CancellationToken ct = default);
    Task UpdateAsync(TUser user, CancellationToken ct = default);
    Task MarkBlockedAsync(long telegramId, CancellationToken ct = default);
}
