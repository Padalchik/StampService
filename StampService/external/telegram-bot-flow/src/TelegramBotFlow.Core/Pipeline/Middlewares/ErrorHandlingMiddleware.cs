using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Pipeline.Middlewares;

internal sealed class ErrorHandlingMiddleware : IUpdateMiddleware
{
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IUpdateResponder _responder;
    private readonly string _errorMessage;

    public ErrorHandlingMiddleware(
        ILogger<ErrorHandlingMiddleware> logger,
        IUpdateResponder responder,
        IOptions<BotMessages> messages)
    {
        _logger = logger;
        _responder = responder;
        _errorMessage = messages.Value.ErrorMessage;
    }

    public async Task InvokeAsync(UpdateContext context, UpdateDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Update processing cancelled for user {UserId}", context.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled exception processing {UpdateType} from user {UserId} on screen {Screen}, handler {Handler}",
                context.UpdateType,
                context.UserId,
                context.Session?.Navigation.CurrentScreen,
                context.HandlerName);

            await TryNotifyUser(context);
            throw;
        }
    }

    private async Task TryNotifyUser(UpdateContext context)
    {
        try
        {
            if (context.ChatId != 0)
            {
                await _responder.ReplyAsync(context, _errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send error notification to user {UserId}", context.UserId);
        }
    }
}