using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Screens;

/// <summary>
/// Единственный публичный мутатор навигационного состояния сессии.
/// Аналог <c>useNavigate()</c> + <c>useLocation()</c> в React Router —
/// вся навигация идёт через один сервис, а не через прямые мутации стека.
/// </summary>
public interface INavigationService
{
    /// <summary>Переходит к экрану по его идентификатору.</summary>
    Task NavigateToAsync(UpdateContext context, string screenId);

    /// <summary>Переходит к экрану по его типу.</summary>
    Task NavigateToAsync<TScreen>(UpdateContext context) where TScreen : IScreen;

    /// <summary>Переходит к экрану по экземпляру типа во время выполнения.</summary>
    Task NavigateToAsync(UpdateContext context, Type screenType);

    /// <summary>
    /// Возвращается на предыдущий экран (pop стека).
    /// Если стек пуст — перерисовывает текущий экран без изменения состояния.
    /// </summary>
    Task NavigateBackAsync(UpdateContext context);

    /// <summary>Перерисовывает текущий экран без изменения стека.</summary>
    Task RefreshScreenAsync(UpdateContext context);

    /// <summary>Показывает произвольное представление без перехода на новый экран.</summary>
    Task ShowViewAsync(UpdateContext context, ScreenView view);

    /// <summary>
    /// Переходит к экрану, полностью сбрасывая историю навигации.
    /// Целевой экран становится «корневым». Используется после завершения визарда или
    /// при возврате в главное меню.
    /// </summary>
    Task NavigateToRootAsync(UpdateContext context, Type screenType);

    /// <summary>
    /// Переходит к экрану по строковому ID, полностью сбрасывая историю навигации.
    /// </summary>
    Task NavigateToRootAsync(UpdateContext context, string screenId);
}