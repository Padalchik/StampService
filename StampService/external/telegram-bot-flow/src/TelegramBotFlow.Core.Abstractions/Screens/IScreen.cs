using TelegramBotFlow.Core.Context;

namespace TelegramBotFlow.Core.Screens;

/// <summary>
/// Контракт экрана диалога, рендерящего состояние UI для пользователя.
/// </summary>
public interface IScreen
{
    /// <summary>
    /// Формирует представление экрана для текущего контекста.
    /// </summary>
    /// <param name="ctx">Контекст update-а.</param>
    /// <returns>Готовое представление экрана.</returns>
    ValueTask<ScreenView> RenderAsync(UpdateContext ctx);
}