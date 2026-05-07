namespace TelegramBotFlow.Core.Sessions;

/// <summary>
/// Пользовательская сессия — агрегирует прикладные данные (<see cref="Data"/>) и
/// состояние навигации (<see cref="Navigation"/>).
///
/// Прикладной код (экраны, визарды) читает/пишет только <see cref="Data"/>.
/// Состояние навигации мутируется исключительно фреймворком через <c>INavigationService</c>.
/// </summary>
public sealed class UserSession
{
    /// <summary>Максимальная глубина стека навигации (default value, actual limit is on NavigationState).</summary>
    public const int MAX_NAVIGATION_DEPTH = 20;

    /// <summary>Идентификатор пользователя Telegram.</summary>
    public long UserId { get; }

    /// <summary>Время создания сессии (UTC).</summary>
    public DateTime CreatedAt { get; internal set; }

    /// <summary>Время последней активности пользователя (UTC).</summary>
    public DateTime LastActivity { get; internal set; }

    /// <summary>
    /// Прикладные данные сессии: key-value хранилище для экранов и визардов.
    /// </summary>
    public SessionData Data { get; } = new();

    /// <summary>
    /// Навигационное состояние: текущий экран, стек, nav message, pending input, wizard id.
    /// Только для чтения из прикладного кода — управляется фреймворком.
    /// </summary>
    public NavigationState Navigation { get; } = new();

    /// <summary>Создаёт новую сессию пользователя.</summary>
    public UserSession(long userId)
    {
        UserId = userId;
        CreatedAt = DateTime.UtcNow;
        LastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Полная очистка сессии: данные + навигационное состояние.
    /// Используется при команде /start для полного сброса.
    /// </summary>
    public void Clear()
    {
        Data.Clear();
        Navigation.Clear();
    }

    /// <summary>
    /// Сбрасывает стек навигации и текущий экран, сохраняя пользовательские данные и
    /// якорное сообщение. Используется при возврате в главное меню.
    /// </summary>
    public void ResetNavigation()
    {
        Navigation.Reset();
    }
}