using Microsoft.Extensions.Options;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

internal sealed class SessionMiddleware : IUpdateMiddleware
{
    private readonly ISessionStore _sessionStore;
    private readonly ISessionLockProvider _lockProvider;
    private readonly BotConfiguration _config;

    public SessionMiddleware(
        ISessionStore sessionStore,
        ISessionLockProvider lockProvider,
        IOptions<BotConfiguration> config)
    {
        _sessionStore = sessionStore;
        _lockProvider = lockProvider;
        _config = config.Value;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        if (context.UserId == 0)
        {
            await next(context);
            return;
        }

        using IDisposable sessionLock = await _lockProvider.AcquireLockAsync(context.UserId, context.CancellationToken);

        UserSession session = await _sessionStore.GetOrCreateAsync(context.UserId, context.CancellationToken);
        session.Navigation.MaxPayloads = _config.PayloadCacheSize;
        session.Navigation.MaxNavigationDepth = _config.MaxNavigationDepth;
        context.Session = session;

        try
        {
            await next(context);
        }
        finally
        {
            await _sessionStore.SaveAsync(session, context.CancellationToken);
        }
    }
}