using Microsoft.EntityFrameworkCore;
using TelegramBotFlow.Core.Users;

namespace TelegramBotFlow.Core.Data;

/// <summary>
/// EF Core implementation of <see cref="IBotUserStore{TUser}"/>.
/// Uses <see cref="BotDbContext{TUser}"/> for persistence.
/// </summary>
public sealed class EfBotUserStore<TUser> : IBotUserStore<TUser>
    where TUser : BotUser, new()
{
    private readonly BotDbContext<TUser> _db;

    public EfBotUserStore(BotDbContext<TUser> db) => _db = db;

    public async Task<TUser?> FindByTelegramIdAsync(long telegramId, CancellationToken ct = default)
        => await _db.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId, ct);

    public async Task CreateAsync(TUser user, CancellationToken ct = default)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TUser user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkBlockedAsync(long telegramId, CancellationToken ct = default)
    {
        TUser? user = await FindByTelegramIdAsync(telegramId, ct);
        if (user is not null)
        {
            user.IsBlocked = true;
            await _db.SaveChangesAsync(ct);
        }
    }
}
