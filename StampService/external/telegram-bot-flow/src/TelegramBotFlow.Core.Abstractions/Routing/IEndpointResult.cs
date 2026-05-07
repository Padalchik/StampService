using System.Text.Json;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Routing;

/// <summary>
/// Результат обработчика маршрута — исполняется через <see cref="BotExecutionContext"/>.
///
/// По аналогии с <c>IResult</c> в ASP.NET Core Minimal API: результат выполняет себя сам,
/// получая все зависимости через типизированный контекст, без Service Locator.
/// Каждый результат сам отвечает на callback query (убирает спиннер с кнопки),
/// а затем выполняет навигацию или другое действие.
/// </summary>
public interface IEndpointResult
{
    Task ExecuteAsync(BotExecutionContext context);
}

/// <summary>
/// Показывает произвольное представление без перехода на новый экран.
/// </summary>
public sealed record ShowViewResult(ScreenView View) : IEndpointResult
{
    public async Task ExecuteAsync(BotExecutionContext ctx)
    {
        if (ctx.Update.Update.CallbackQuery is not null)
            await ctx.Responder.AnswerCallbackAsync(ctx.Update);
        await ctx.Navigator.ShowViewAsync(ctx.Update, View);
    }
}

/// <summary>
/// Возвращает пользователя на предыдущий экран (якорная навигация назад).
/// Опциональный <see cref="Notification"/> отображается как toast-уведомление над кнопкой.
/// </summary>
public sealed record NavigateBackResult(string? Notification = null) : IEndpointResult
{
    internal static readonly NavigateBackResult s_default = new();

    public async Task ExecuteAsync(BotExecutionContext ctx)
    {
        if (ctx.Update.Update.CallbackQuery is not null)
            await ctx.Responder.AnswerCallbackAsync(ctx.Update, Notification);
        await ctx.Navigator.NavigateBackAsync(ctx.Update);
    }
}

/// <summary>
/// Переходит к экрану указанного типа.
/// Опциональные <see cref="NavArgs"/> передаются целевому экрану через <c>NavigationState</c>.
/// </summary>
public sealed record NavigateToResult(Type ScreenType, Dictionary<string, string>? NavArgs = null) : IEndpointResult
{
    /// <summary>
    /// Добавляет строковый аргумент навигации.
    /// </summary>
    public NavigateToResult WithArg(string key, string value) =>
        new(ScreenType, new Dictionary<string, string>(NavArgs ?? []) { [key] = value });

    /// <summary>
    /// Добавляет типизированный аргумент навигации (сериализуется в JSON).
    /// </summary>
    public NavigateToResult WithArg<T>(string key, T value) =>
        WithArg(key, JsonSerializer.Serialize(value));

    public async Task ExecuteAsync(BotExecutionContext ctx)
    {
        if (ctx.Update.Update.CallbackQuery is not null)
            await ctx.Responder.AnswerCallbackAsync(ctx.Update);
        if (NavArgs is not null)
            ctx.Update.Session?.Navigation.PopulateNavArgs(NavArgs);
        await ctx.Navigator.NavigateToAsync(ctx.Update, ScreenType);
    }
}

/// <summary>
/// Перерисовывает текущий экран без изменения стека навигации.
/// Опциональный <see cref="Notification"/> отображается как toast-уведомление.
/// </summary>
public sealed record RefreshResult(string? Notification = null) : IEndpointResult
{
    public async Task ExecuteAsync(BotExecutionContext ctx)
    {
        if (ctx.Update.Update.CallbackQuery is not null)
            await ctx.Responder.AnswerCallbackAsync(ctx.Update, Notification);
        await ctx.Navigator.RefreshScreenAsync(ctx.Update);
    }
}

/// <summary>
/// Остаётся в текущем состоянии ввода — <c>PendingInputActionId</c> сохраняется.
/// Опционально удаляет сообщение пользователя и показывает уведомление в callback.
/// </summary>
public sealed record StayResult(string? Notification = null, bool DeleteMessage = true) : IEndpointResult
{
    public async Task ExecuteAsync(BotExecutionContext ctx)
    {
        // Восстанавливаем ожидание ввода — HandlerDelegateFactory сбросил его до вызова хэндлера.
        // PendingActionId задаётся фреймворком только для input-хэндлеров.
        if (ctx.PendingActionId is not null)
            ctx.Update.Session?.Navigation.SetPending(ctx.PendingActionId);

        if (DeleteMessage && ctx.Update.Update.Message is not null)
            await ctx.Responder.DeleteMessageAsync(ctx.Update);

        if (Notification is not null)
            await ctx.Responder.AnswerCallbackAsync(ctx.Update, Notification);
    }
}

/// <summary>
/// Только отвечает на callback query (убирает спиннер) — для side-effect-only хэндлеров.
/// </summary>
public sealed record EmptyResult : IEndpointResult
{
    public static readonly EmptyResult Instance = new();

    public Task ExecuteAsync(BotExecutionContext ctx) =>
        ctx.Update.Update.CallbackQuery is not null
            ? ctx.Responder.AnswerCallbackAsync(ctx.Update)
            : Task.CompletedTask;
}

/// <summary>
/// Переходит к экрану по строковому идентификатору.
/// Опциональные <see cref="NavArgs"/> передаются целевому экрану через <c>NavigationState</c>.
/// </summary>
public sealed record NavigateToByIdResult(string ScreenId, Dictionary<string, string>? NavArgs = null) : IEndpointResult
{
    /// <summary>
    /// Добавляет строковый аргумент навигации.
    /// </summary>
    public NavigateToByIdResult WithArg(string key, string value) =>
        new(ScreenId, new Dictionary<string, string>(NavArgs ?? []) { [key] = value });

    /// <summary>
    /// Добавляет типизированный аргумент навигации (сериализуется в JSON).
    /// </summary>
    public NavigateToByIdResult WithArg<T>(string key, T value) =>
        WithArg(key, JsonSerializer.Serialize(value));

    public async Task ExecuteAsync(BotExecutionContext ctx)
    {
        if (ctx.Update.Update.CallbackQuery is not null)
            await ctx.Responder.AnswerCallbackAsync(ctx.Update);
        if (NavArgs is not null)
            ctx.Update.Session?.Navigation.PopulateNavArgs(NavArgs);
        await ctx.Navigator.NavigateToAsync(ctx.Update, ScreenId);
    }
}

/// <summary>
/// Переходит к экрану с очисткой всей истории навигации.
/// Опциональные <see cref="NavArgs"/> передаются целевому экрану через <c>NavigationState</c>.
/// </summary>
public sealed record NavigateToRootResult(Type ScreenType, Dictionary<string, string>? NavArgs = null) : IEndpointResult
{
    /// <summary>
    /// Добавляет строковый аргумент навигации.
    /// </summary>
    public NavigateToRootResult WithArg(string key, string value) =>
        new(ScreenType, new Dictionary<string, string>(NavArgs ?? []) { [key] = value });

    /// <summary>
    /// Добавляет типизированный аргумент навигации (сериализуется в JSON).
    /// </summary>
    public NavigateToRootResult WithArg<T>(string key, T value) =>
        WithArg(key, JsonSerializer.Serialize(value));

    public async Task ExecuteAsync(BotExecutionContext ctx)
    {
        if (ctx.Update.Update.CallbackQuery is not null)
            await ctx.Responder.AnswerCallbackAsync(ctx.Update);
        if (NavArgs is not null)
            ctx.Update.Session?.Navigation.PopulateNavArgs(NavArgs);
        await ctx.Navigator.NavigateToRootAsync(ctx.Update, ScreenType);
    }
}

/// <summary>
/// Переходит к экрану по строковому ID с очисткой всей истории навигации.
/// Опциональные <see cref="NavArgs"/> передаются целевому экрану через <c>NavigationState</c>.
/// </summary>
public sealed record NavigateToRootByIdResult(string ScreenId, Dictionary<string, string>? NavArgs = null) : IEndpointResult
{
    /// <summary>
    /// Добавляет строковый аргумент навигации.
    /// </summary>
    public NavigateToRootByIdResult WithArg(string key, string value) =>
        new(ScreenId, new Dictionary<string, string>(NavArgs ?? []) { [key] = value });

    /// <summary>
    /// Добавляет типизированный аргумент навигации (сериализуется в JSON).
    /// </summary>
    public NavigateToRootByIdResult WithArg<T>(string key, T value) =>
        WithArg(key, JsonSerializer.Serialize(value));

    public async Task ExecuteAsync(BotExecutionContext ctx)
    {
        if (ctx.Update.Update.CallbackQuery is not null)
            await ctx.Responder.AnswerCallbackAsync(ctx.Update);
        if (NavArgs is not null)
            ctx.Update.Session?.Navigation.PopulateNavArgs(NavArgs);
        await ctx.Navigator.NavigateToRootAsync(ctx.Update, ScreenId);
    }
}

/// <summary>
/// Запускает визард заданного типа через <see cref="IWizardLauncher"/>.
/// Не зависит от <c>WizardRegistry</c> или <c>IWizardStore</c> напрямую.
/// </summary>
public sealed record StartWizardResult(Type WizardType) : IEndpointResult
{
    public async Task ExecuteAsync(BotExecutionContext ctx)
    {
        if (ctx.Wizards is null)
            throw new InvalidOperationException(
                "Wizard support is not registered. Call AddWizards() to enable it.");

        IEndpointResult? firstResult = await ctx.Wizards.LaunchAsync(WizardType, ctx.Update);

        if (firstResult is not null)
            await firstResult.ExecuteAsync(ctx);
        else if (ctx.Update.Update.CallbackQuery is not null)
            await ctx.Responder.AnswerCallbackAsync(ctx.Update);
    }
}

/// <summary>
/// Показывает toast-уведомление над кнопкой без навигации и изменения экрана.
/// Используется для side-effect-only хэндлеров, которым нужно лишь сообщить пользователю
/// о результате действия.
/// </summary>
public sealed record AnswerCallbackResult(string? Text = null) : IEndpointResult
{
    public Task ExecuteAsync(BotExecutionContext ctx) =>
        ctx.Responder.AnswerCallbackAsync(ctx.Update, Text);
}