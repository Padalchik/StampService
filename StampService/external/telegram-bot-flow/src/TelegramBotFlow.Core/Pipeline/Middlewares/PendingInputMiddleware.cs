using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Routing;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

internal sealed class PendingInputMiddleware : IUpdateMiddleware
{
    private readonly InputHandlerRegistry _registry;
    private readonly ILogger<PendingInputMiddleware> _logger;

    public PendingInputMiddleware(InputHandlerRegistry registry, ILogger<PendingInputMiddleware> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        // Callbacks проходят без изменений — NavigationHandler очищает pending сам
        if (context.UpdateType != UpdateType.Message)
        {
            await next(context);
            return;
        }

        // Команды сбрасывают pending и передают управление роутеру
        if (context.MessageText?.StartsWith('/') == true)
        {
            if (context.Session is not null)
                context.Session.Navigation.PendingInputActionId = null;

            await next(context);
            return;
        }

        string? actionId = context.Session?.Navigation.PendingInputActionId;
        if (actionId is null)
        {
            await next(context);
            return;
        }

        UpdateDelegate? handler = _registry.Find(actionId);
        if (handler is null)
        {
            _logger.LogWarning("Input handler '{ActionId}' not found, falling through to router", actionId);

            if (context.Session is not null)
                context.Session.Navigation.PendingInputActionId = null;

            await next(context);
            return;
        }

        await handler(context);
    }
}