using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Context;

/// <summary>
/// Контекст выполнения результата эндпоинта. Передаётся в <c>IEndpointResult.ExecuteAsync</c>.
///
/// Аналог <c>HttpContext</c> в ASP.NET Core Minimal API <c>IResult.ExecuteAsync(HttpContext)</c>:
/// результат выполняет себя сам, но получает все зависимости через явные типизированные
/// свойства, а не через IServiceProvider (Service Locator антипаттерн).
/// </summary>
public sealed class BotExecutionContext
{
    /// <summary>Контекст входящего Telegram update.</summary>
    public UpdateContext Update { get; }

    /// <summary>
    /// Единственный мутатор навигации. Используй для переходов между экранами.
    /// Аналог <c>useNavigate()</c> в React Router.
    /// </summary>
    public INavigationService Navigator { get; }

    /// <summary>Сервис ответов пользователю через Telegram API.</summary>
    public IUpdateResponder Responder { get; }

    /// <summary>
    /// Сервис запуска визардов. <see langword="null"/>, если wizards не зарегистрированы
    /// (AddWizards не вызван).
    /// </summary>
    public IWizardLauncher? Wizards { get; }

    /// <summary>
    /// ID input-действия, которое ожидает ответа пользователя.
    /// Задаётся фреймворком только для input-хэндлеров (MapInput).
    /// <see cref="StayResult"/> использует это значение
    /// для восстановления ожидания после обработки.
    /// </summary>
    public string? PendingActionId { get; }

    public BotExecutionContext(
        UpdateContext update,
        INavigationService navigator,
        IUpdateResponder responder,
        IWizardLauncher? wizards = null,
        string? pendingActionId = null)
    {
        Update = update;
        Navigator = navigator;
        Responder = responder;
        Wizards = wizards;
        PendingActionId = pendingActionId;
    }

    /// <summary>
    /// Создаёт <see cref="BotExecutionContext"/> из <see cref="UpdateContext"/>, резолвя
    /// зависимости из scoped DI-контейнера. Единственная точка использования
    /// IServiceProvider — инфраструктурный шов фреймворка.
    /// </summary>
    public static BotExecutionContext FromUpdateContext(UpdateContext ctx, string? pendingActionId = null) => new(
        ctx,
        ctx.RequestServices.GetRequiredService<INavigationService>(),
        ctx.RequestServices.GetRequiredService<IUpdateResponder>(),
        ctx.RequestServices.GetService<IWizardLauncher>(),
        pendingActionId);
}