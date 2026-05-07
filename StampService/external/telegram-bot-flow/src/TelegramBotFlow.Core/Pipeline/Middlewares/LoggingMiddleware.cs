using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

internal sealed class LoggingMiddleware : IUpdateMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "Processing {UpdateType} from user {UserId} in chat {ChatId}. Text: {Text}, Callback: {Callback}",
            context.UpdateType,
            context.UserId,
            context.ChatId,
            context.MessageText ?? "(none)",
            context.CallbackData ?? "(none)");

        await next(context);

        sw.Stop();

        _logger.LogInformation(
            "Processed {UpdateType} from user {UserId} in {ElapsedMs}ms",
            context.UpdateType,
            context.UserId,
            sw.ElapsedMilliseconds);
    }
}