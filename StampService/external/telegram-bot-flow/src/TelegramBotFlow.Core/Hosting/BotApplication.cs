using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Extensions;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Hosting;

/// <summary>
/// Центральный API регистрации маршрутов и запуска Telegram-бота.
/// Middleware регистрируется через <see cref="BotApplicationBuilder"/>.
/// </summary>
public sealed class BotApplication
{
    private readonly WebApplication _app;
    private readonly UpdateRouter _router;
    private readonly List<Func<UpdateDelegate, UpdateDelegate>> _middlewares;
    private readonly List<string> _registeredMiddleware;
    private MenuBuilder? _menuBuilder;

    /// <summary>
    /// Провайдер сервисов приложения.
    /// </summary>
    public IServiceProvider Services => _app.Services;

    /// <summary>
    /// Базовое ASP.NET Core приложение для webhook- и инфраструктурных endpoint-ов.
    /// </summary>
    public WebApplication WebApp => _app;

    private BotApplication(
        WebApplication app,
        UpdateRouter router,
        List<Func<UpdateDelegate, UpdateDelegate>> middlewares,
        List<string> registeredMiddleware)
    {
        _app = app;
        _router = router;
        _middlewares = middlewares;
        _registeredMiddleware = registeredMiddleware;
    }

    /// <summary>
    /// Создаёт builder приложения бота.
    /// </summary>
    /// <param name="args">Аргументы командной строки.</param>
    /// <returns>Builder приложения бота.</returns>
    public static BotApplicationBuilder CreateBuilder(string[] args) => new(args);

    /// <summary>
    /// Собирает экземпляр приложения с регистрацией базовых сервисов фреймворка.
    /// </summary>
    /// <param name="builder">Builder приложения.</param>
    /// <returns>Собранный экземпляр приложения бота.</returns>
    public static BotApplication Build(BotApplicationBuilder builder)
    {
        builder.Services.AddTelegramBotFlow(builder.Configuration);

        WebApplication app = builder.WebAppBuilder.Build();

        UpdateRouter router = app.Services.GetRequiredService<UpdateRouter>();

        return new BotApplication(app, router, builder.Middlewares, builder.RegisteredMiddleware);
    }

    // -- Route registration (Minimal API style with DI) --

    /// <summary>
    /// Регистрирует обработчик команды.
    /// </summary>
    /// <param name="command">Текст команды с или без ведущего символа <c>/</c>.</param>
    /// <param name="handler">Делегат обработчика.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication MapCommand(string command, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Command(command, HandlerDelegateFactory.Create(handler)));
        return this;
    }

    /// <summary>
    /// Maps a deep link handler for <c>/command</c> with payload (e.g. <c>/start ref_abc</c>).
    /// Higher priority than <see cref="MapCommand"/>; the payload is available via
    /// <see cref="UpdateContext.CommandArgument"/>.
    /// </summary>
    /// <param name="command">Command with or without leading <c>/</c>.</param>
    /// <param name="handler">Handler delegate.</param>
    /// <returns>Current instance for fluent configuration.</returns>
    public BotApplication MapDeepLink(string command, Delegate handler)
    {
        var route = RouteEntry.DeepLink(command, HandlerDelegateFactory.Create(handler));
        _router.AddRoute(route);
        return this;
    }

    /// <summary>
    /// Регистрирует обработчик callback-data по шаблону.
    /// </summary>
    /// <param name="pattern">Шаблон callback-data, включая wildcard-суффикс <c>*</c>.</param>
    /// <param name="handler">Делегат обработчика.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication MapCallback(string pattern, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Callback(pattern, HandlerDelegateFactory.Create(handler)));
        return this;
    }

    /// <summary>
    /// Регистрирует типизированный action-обработчик.
    /// Callback ID определяется через <see cref="ActionIdResolver"/> (учитывает <see cref="ActionIdAttribute"/>).
    /// </summary>
    public BotApplication MapAction<TAction>(Delegate handler) where TAction : IBotAction
        => MapAction(ActionIdResolver.GetId<TAction>(), handler);

    /// <summary>
    /// Регистрирует типизированный action-обработчик, ожидающий payload.
    /// Отрабатывает маршрут <c>TActionName:*</c> (где * — это shortId payload).
    /// Action ID определяется через <see cref="ActionIdResolver"/> (учитывает <see cref="ActionIdAttribute"/>).
    /// </summary>
    public BotApplication MapAction<TAction, TPayload>(Delegate handler) where TAction : IBotAction
    {
        string callbackId = ActionIdResolver.GetId<TAction>();
        UpdateDelegate inner = PayloadDelegateFactory.Create<TPayload>(handler, callbackId);
        _router.AddRoute(RouteEntry.Callback($"{callbackId}:*", inner));
        return this;
    }

    /// <summary>
    /// Регистрирует обработчик action-кнопки.
    /// Результат хэндлера сам отвечает на callback (<see cref="IEndpointResult.ExecuteAsync"/>).
    /// Обработчик должен возвращать <c>Task&lt;IEndpointResult&gt;</c>.
    /// </summary>
    public BotApplication MapAction(string callbackId, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Callback(callbackId, HandlerDelegateFactory.Create(handler)));
        return this;
    }

    /// <summary>
    /// Регистрирует обработчик callback-группы по префиксу <c>{prefix}:*</c>.
    /// </summary>
    /// <param name="prefix">Префикс callback-data.</param>
    /// <param name="handler">Делегат обработчика группы callback.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication MapCallbackGroup(string prefix, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Callback($"{prefix}:*",
            HandlerDelegateFactory.CreateForCallbackGroup(handler, prefix)));
        return this;
    }

    /// <summary>
    /// Встраивает обработку навигационных callback-ов <c>nav:*</c> во фреймворк.
    /// Поддерживает back, close, menu и динамический переход по screenId.
    /// Вызывать до <c>MapBotEndpoints()</c>.
    /// </summary>
    /// <typeparam name="TMenuScreen">Тип экрана главного меню для команды <c>nav:menu</c>.</typeparam>
    public BotApplication UseNavigation<TMenuScreen>() where TMenuScreen : IScreen
    {
        Type menuScreenType = typeof(TMenuScreen);
        return MapCallbackGroup("nav", (UpdateContext ctx, string screenId) =>
        {
            IEndpointResult result = screenId switch
            {
                "back" => BotResults.Back(),
                "close" => BotResults.Refresh(),
                "menu" => new NavigateToRootResult(menuScreenType),
                _ => BotResults.NavigateTo(screenId)
            };
            return Task.FromResult(result);
        });
    }

    /// <summary>
    /// Регистрирует типизированный input-обработчик.
    /// Action ID определяется через <see cref="ActionIdResolver"/> (учитывает <see cref="ActionIdAttribute"/>).
    /// </summary>
    public BotApplication MapInput<TAction>(Delegate handler) where TAction : IBotAction
        => MapInput(ActionIdResolver.GetId<TAction>(), handler);

    /// <summary>
    /// Registers an input handler for the given <paramref name="actionId"/>.
    /// The handler is invoked when <c>session.PendingInputActionId == actionId</c> and the
    /// user sends a text message. The handler must return
    /// <c>Task&lt;IEndpointResult&gt;</c>.
    /// </summary>
    public BotApplication MapInput(string actionId, Delegate handler)
    {
        InputHandlerRegistry registry = Services.GetRequiredService<InputHandlerRegistry>();
        registry.Register(actionId, HandlerDelegateFactory.CreateForInput(handler, actionId));
        return this;
    }

    /// <summary>
    /// Регистрирует обработчик текстовых сообщений по предикату.
    /// </summary>
    /// <param name="predicate">Условие сопоставления update-а.</param>
    /// <param name="handler">Делегат обработчика.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication MapMessage(Func<UpdateContext, bool> predicate, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Message(predicate, HandlerDelegateFactory.Create(handler)));
        return this;
    }

    /// <summary>
    /// Registers a handler for <see cref="Telegram.Bot.Types.Update.MyChatMember"/> updates
    /// (e.g. when a user blocks/unblocks the bot).
    /// </summary>
    /// <param name="handler">Handler delegate.</param>
    /// <returns>Current instance for fluent configuration.</returns>
    public BotApplication MapChatMember(Delegate handler)
    {
        var route = RouteEntry.ChatMember(HandlerDelegateFactory.Create(handler));
        _router.AddRoute(route);
        return this;
    }

    /// <summary>
    /// Регистрирует обработчик произвольного update-а по предикату.
    /// </summary>
    /// <param name="predicate">Условие сопоставления update-а.</param>
    /// <param name="handler">Делегат обработчика.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication MapUpdate(Func<UpdateContext, bool> predicate, Delegate handler)
    {
        _router.AddRoute(RouteEntry.Update(predicate, HandlerDelegateFactory.Create(handler)));
        return this;
    }

    /// <summary>
    /// Регистрирует fallback-обработчик при отсутствии совпавшего маршрута.
    /// </summary>
    /// <param name="handler">Fallback-делегат.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication MapFallback(Delegate handler)
    {
        _router.SetFallback(HandlerDelegateFactory.Create(handler));
        return this;
    }

    // -- Menu --

    /// <summary>
    /// Задаёт меню команд бота, отображаемое в Telegram.
    /// </summary>
    /// <param name="configure">Колбэк конфигурации меню.</param>
    /// <returns>Текущий экземпляр приложения для fluent-конфигурации.</returns>
    public BotApplication SetMenu(Action<MenuBuilder> configure)
    {
        _menuBuilder = new MenuBuilder();
        configure(_menuBuilder);
        return this;
    }

    // -- Run --

    /// <summary>
    /// Собирает pipeline и запускает runtime бота.
    /// </summary>
    /// <returns>Задача жизненного цикла приложения бота.</returns>
    public async Task RunAsync()
    {
        ValidateMiddlewareOrder();
        var pipeline = UpdatePipeline.Build(_middlewares, _router.BuildTerminal());
        var runtime = new BotRuntime(_app);
        await runtime.RunAsync(pipeline, _menuBuilder);
    }

    internal void ValidateMiddlewareOrder() =>
        MiddlewareOrderValidator.Validate(_registeredMiddleware);
}

internal sealed class PipelineHolder
{
    /// <summary>
    /// Экземпляр pipeline, доступный инфраструктурным компонентам.
    /// </summary>
    public UpdatePipeline Pipeline { get; set; } = UpdatePipeline.Build([], _ => Task.CompletedTask);
}