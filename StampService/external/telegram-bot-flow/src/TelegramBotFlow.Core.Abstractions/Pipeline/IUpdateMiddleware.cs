using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Pipeline;

/// <summary>
/// Контракт middleware для обработки Telegram update-а в pipeline.
/// </summary>
public interface IUpdateMiddleware
{
    /// <summary>
    /// Выполняет middleware-логику и передаёт управление следующему шагу.
    /// </summary>
    /// <param name="context">Контекст текущего update-а.</param>
    /// <param name="next">Следующий делегат в pipeline.</param>
    Task InvokeAsync(UpdateContext context, UpdateDelegate next);
}