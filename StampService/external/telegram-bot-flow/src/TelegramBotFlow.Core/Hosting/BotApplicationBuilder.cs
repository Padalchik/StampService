using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Endpoints;
using TelegramBotFlow.Core.Extensions;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Pipeline.Middlewares;

namespace TelegramBotFlow.Core.Hosting;

/// <summary>
/// Конфигуратор и фабрика экземпляра <see cref="BotApplication"/>.
/// </summary>
public sealed class BotApplicationBuilder
{
    internal readonly List<Func<UpdateDelegate, UpdateDelegate>> Middlewares = [];
    internal readonly List<string> RegisteredMiddleware = [];

    /// <summary>
    /// Базовый web-builder приложения.
    /// </summary>
    public WebApplicationBuilder WebAppBuilder { get; }

    /// <summary>
    /// Коллекция сервисов DI-контейнера.
    /// </summary>
    public IServiceCollection Services => WebAppBuilder.Services;

    /// <summary>
    /// Конфигурация приложения.
    /// </summary>
    public ConfigurationManager Configuration => WebAppBuilder.Configuration;

    internal BotApplicationBuilder(string[] args)
    {
        WebAppBuilder = WebApplication.CreateBuilder(args);
    }

    // -- Middleware registration --

    /// <summary>
    /// Добавляет middleware-фабрику в pipeline обработки update-ов.
    /// </summary>
    /// <param name="middleware">Фабрика middleware-делегата.</param>
    /// <returns>Текущий builder для fluent-конфигурации.</returns>
    public BotApplicationBuilder Use(Func<UpdateDelegate, UpdateDelegate> middleware)
    {
        Middlewares.Add(middleware);
        return this;
    }

    /// <summary>
    /// Добавляет middleware, резолвимый из DI-контейнера.
    /// </summary>
    /// <typeparam name="TMiddleware">Тип middleware, реализующий <see cref="IUpdateMiddleware"/>.</typeparam>
    /// <returns>Текущий builder для fluent-конфигурации.</returns>
    public BotApplicationBuilder Use<TMiddleware>() where TMiddleware : IUpdateMiddleware
    {
        Middlewares.Add(next => async context =>
        {
            TMiddleware middleware = context.RequestServices.GetRequiredService<TMiddleware>();
            await middleware.InvokeAsync(context, next);
        });

        return this;
    }

    /// <summary>
    /// Adds a conditional middleware branch that executes only when the predicate returns <c>true</c>.
    /// The branch rejoins the main pipeline after execution (same semantics as ASP.NET Core UseWhen).
    /// </summary>
    /// <param name="predicate">Predicate evaluated per update to decide whether the branch executes.</param>
    /// <param name="configureBranch">Action to configure middleware in the conditional branch.</param>
    /// <returns>This builder for fluent chaining.</returns>
    public BotApplicationBuilder UseWhen(
        Func<UpdateContext, bool> predicate,
        Action<MiddlewareBranchBuilder> configureBranch)
    {
        var branchBuilder = new MiddlewareBranchBuilder(Services);
        configureBranch(branchBuilder);
        Middlewares.Add(ConditionalMiddleware.Create(predicate, branchBuilder.Middlewares));
        return this;
    }

    /// <summary>
    /// Добавляет middleware глобальной обработки исключений.
    /// </summary>
    /// <returns>Текущий builder для fluent-конфигурации.</returns>
    public BotApplicationBuilder UseErrorHandling()
    {
        RegisteredMiddleware.Add("error_handling");
        return Use<ErrorHandlingMiddleware>();
    }

    /// <summary>
    /// Добавляет middleware логирования обработки update-ов.
    /// </summary>
    /// <returns>Текущий builder для fluent-конфигурации.</returns>
    public BotApplicationBuilder UseLogging() => Use<LoggingMiddleware>();

    /// <summary>
    /// Добавляет middleware, блокирующий запросы не из личных чатов (группы, другие каналы).
    /// </summary>
    /// <returns>Текущий builder для fluent-конфигурации.</returns>
    public BotApplicationBuilder UsePrivateChatOnly() => Use<PrivateChatOnlyMiddleware>();

    /// <summary>
    /// Добавляет middleware загрузки и сохранения пользовательской сессии.
    /// </summary>
    /// <returns>Текущий builder для fluent-конфигурации.</returns>
    public BotApplicationBuilder UseSession()
    {
        RegisteredMiddleware.Add("session");
        return Use<SessionMiddleware>();
    }

    /// <summary>
    /// Добавляет middleware обработки визардов (форм). Перехватывает update-ы для активного визарда.
    /// Должен быть добавлен после UseSession().
    /// </summary>
    /// <returns>Текущий builder для fluent-конфигурации.</returns>
    public BotApplicationBuilder UseWizards()
    {
        RegisteredMiddleware.Add("wizards");
        return Use<WizardMiddleware>();
    }

    /// <summary>
    /// Добавляет middleware вычисления административного доступа пользователя.
    /// </summary>
    /// <returns>Текущий builder для fluent-конфигурации.</returns>
    public BotApplicationBuilder UseAccessPolicy() => Use<AccessPolicyMiddleware>();

    /// <summary>
    /// Adds a middleware that intercepts incoming text messages and routes them to the
    /// registered input handler when <c>session.PendingInputActionId</c> is set.
    /// Must be added after <c>UseSession()</c>.
    /// </summary>
    public BotApplicationBuilder UsePendingInput()
    {
        RegisteredMiddleware.Add("pending_input");
        return Use<PendingInputMiddleware>();
    }

    // -- Build --

    /// <summary>
    /// Builds the <see cref="BotApplication"/>.
    /// Automatically discovers and registers <see cref="IBotEndpoint"/> implementations,
    /// <see cref="Screens.IScreen"/> implementations and <c>IBotWizard</c> implementations
    /// from the entry assembly — only if that assembly actually contains such types.
    ///
    /// This check prevents overwriting explicit service registrations when the application is
    /// started from a test host, where <see cref="Assembly.GetEntryAssembly"/> returns the
    /// test runner (e.g. xunit) rather than the application assembly.
    /// </summary>
    public BotApplication Build()
    {
        Assembly? entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is not null)
        {
            // AddBotEndpoints is safe to call unconditionally (uses TryAddEnumerable)
            Services.AddBotEndpoints(entryAssembly);

            // Only register screens if the entry assembly actually contains IScreen types.
            // Without this check, AddScreens(testRunnerAssembly) would replace the explicitly
            // registered ScreenRegistry with an empty one.
            if (Screens.ScreenRegistry.GetScreenTypes(entryAssembly).Any())
                Services.AddScreens(entryAssembly);

            // Same guard for wizards.
            if (Wizards.WizardRegistry.GetWizardTypes(entryAssembly).Any())
                Services.AddWizards(entryAssembly);
        }

        return BotApplication.Build(this);
    }
}