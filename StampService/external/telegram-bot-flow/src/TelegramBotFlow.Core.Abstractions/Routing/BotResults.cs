using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Routing;

/// <summary>
/// Статическая фабрика результатов обработчиков.
/// Аналог <c>Results</c> в ASP.NET Core Minimal APIs.
/// </summary>
/// <example>
/// <code>
/// app.MapAction("btn", async (BotDbContext db) => {
///     var data = await db.GetAsync();
///     return BotResults.ShowView(new ScreenView($"{data}"));
/// });
///
/// app.MapInput("id", async (UpdateContext ctx, BotDbContext db) => {
///     await db.SaveChangesAsync();
///     return BotResults.Back("✅ Сохранено");
/// });
/// </code>
/// </example>
public static class BotResults
{
    /// <summary>Показывает произвольное представление без навигации.</summary>
    public static IEndpointResult ShowView(ScreenView view) => new ShowViewResult(view);

    /// <summary>Возвращает пользователя на предыдущий экран.</summary>
    public static IEndpointResult Back(string? notification = null) => new NavigateBackResult(notification);

    /// <summary>
    /// Остаётся в текущем состоянии ввода, сохраняя ожидание.
    /// По умолчанию удаляет сообщение пользователя; задайте <c>deleteMessage: false</c>,
    /// чтобы только ответить на callback, не удаляя ввод.
    /// </summary>
    public static IEndpointResult Stay(string? notification = null, bool deleteMessage = true) =>
        new StayResult(notification, deleteMessage);

    /// <summary>Переходит к экрану указанного типа (добавляет в стек навигации).</summary>
    public static IEndpointResult NavigateTo<TScreen>() where TScreen : IScreen =>
        new NavigateToResult(typeof(TScreen));

    /// <summary>
    /// Переходит к экрану с очисткой всей истории навигации.
    /// Целевой экран становится «корневым». Удобно после завершения визарда или критичного действия.
    /// </summary>
    public static IEndpointResult NavigateToRoot<TScreen>() where TScreen : IScreen =>
        new NavigateToRootResult(typeof(TScreen));

    /// <summary>Переходит к экрану по строковому ID с очисткой истории навигации.</summary>
    public static IEndpointResult NavigateToRoot(string screenId) => new NavigateToRootByIdResult(screenId);

    /// <summary>Перерисовывает текущий экран без изменения стека навигации.</summary>
    public static IEndpointResult Refresh(string? notification = null) => new RefreshResult(notification);

    /// <summary>Ничего не делает — используется для side-effect-only хэндлеров.</summary>
    public static IEndpointResult Empty() => EmptyResult.Instance;

    /// <summary>Переходит к экрану по строковому идентификатору (добавляет в стек).</summary>
    public static IEndpointResult NavigateTo(string screenId) => new NavigateToByIdResult(screenId);

    /// <summary>
    /// Запускает визард заданного типа. Инициализирует первый шаг и активирует визард в сессии.
    /// </summary>
    public static IEndpointResult StartWizard<TWizard>() where TWizard : IBotWizard =>
        new StartWizardResult(typeof(TWizard));

    /// <summary>
    /// Отвечает на callback query (показывает toast-уведомление) без навигации.
    /// Используется для side-effect-only хэндлеров, которым нужно лишь показать
    /// результат действия пользователю.
    /// </summary>
    public static IEndpointResult AnswerCallback(string? text = null) => new AnswerCallbackResult(text);
}