using Microsoft.Extensions.Caching.Memory;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Users;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

/// <summary>
/// Middleware that tracks new users via <see cref="IBotUserStore{TUser}"/>.
/// Generic version — use with custom user types implementing <see cref="IBotUser"/>.
/// </summary>
public sealed class UserTrackingMiddleware<TUser> : IUpdateMiddleware
    where TUser : class, IBotUser, new()
{
    private static readonly MemoryCache _knownUsers = new(new MemoryCacheOptions());
    private static readonly MemoryCacheEntryOptions _cacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(1)
    };

    private readonly IBotUserStore<TUser> _userStore;

    public UserTrackingMiddleware(IBotUserStore<TUser> userStore)
    {
        _userStore = userStore;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        if (context.Update.MyChatMember is { } chatMember
            && chatMember.NewChatMember.Status == ChatMemberStatus.Kicked)
        {
            await _userStore.MarkBlockedAsync(chatMember.From.Id, context.CancellationToken);
            await next(context);
            return;
        }

        if (context.UserId != 0)
        {
            long userId = context.UserId;
            if (!_knownUsers.TryGetValue(userId, out TUser? cached))
            {
                TUser? existing = await _userStore.FindByTelegramIdAsync(userId, context.CancellationToken);
                if (existing is null)
                {
                    existing = new TUser { TelegramId = userId };
                    await _userStore.CreateAsync(existing, context.CancellationToken);
                }

                _knownUsers.Set(userId, existing, _cacheOptions);
                context.User = existing;
            }
            else
            {
                context.User = cached;
            }
        }

        await next(context);
    }
}
