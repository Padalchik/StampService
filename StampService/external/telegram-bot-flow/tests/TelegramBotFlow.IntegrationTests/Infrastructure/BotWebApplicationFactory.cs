using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBotFlow.App.Features.MainMenu.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Data;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Users;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory для интеграционных тестов.
/// Заменяет <see cref="IUpdateResponder"/> на мок — позволяет верифицировать
/// вызовы ответов навигации (ReplyAsync, EditMessageAsync, AnswerCallbackAsync и т.д.)
/// без реальных обращений к Telegram Bot API.
/// </summary>
public class BotWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Мок <see cref="IUpdateResponder"/>, регистрируемый в DI вместо реальной реализации.
    /// Все методы возвращают разумные значения по умолчанию.
    /// </summary>
    public IUpdateResponder MockResponder { get; } = Substitute.For<IUpdateResponder>();

    public BotWebApplicationFactory()
    {
        ConfigureMockDefaults();
    }

    private void ConfigureMockDefaults()
    {
        var defaultMsg = new Message { Id = 42, Date = DateTime.UtcNow };

        // ReplyAsync должен вернуть реальный Message — NavigationService сохраняет msg.Id как NavMessageId
        MockResponder
            .ReplyAsync(Arg.Any<UpdateContext>(), Arg.Any<string>())
            .ReturnsForAnyArgs(Task.FromResult(defaultMsg));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Bot:Token"] = "fake-token-for-testing",
                ["Bot:Mode"] = "Polling"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Stub ITelegramBotClient — нужен для startup, но не вызывается напрямую
            // (UpdateResponder заменён нашим MockResponder)
            services.RemoveAll<ITelegramBotClient>();
            services.AddSingleton(Substitute.For<ITelegramBotClient>());

            // Заменяем IUpdateResponder на мок
            services.RemoveAll<IUpdateResponder>();
            services.AddScoped(_ => MockResponder);

            // Заменяем IScreenMessageRenderer на тестовый double —
            // возвращает Message без реальных вызовов Telegram API
            services.RemoveAll<IScreenMessageRenderer>();
            services.AddSingleton<IScreenMessageRenderer, FakeScreenMessageRenderer>();

            // Останавливаем фоновые hosted-сервисы (polling, workers)
            services.RemoveAll(typeof(Microsoft.Extensions.Hosting.IHostedService));

            // In-memory БД вместо PostgreSQL
            services.RemoveAll<BotDbContext<BotUser>>();
            services.RemoveAll<BotDbContext>();

            var options = new DbContextOptionsBuilder<BotDbContext<BotUser>>()
                .UseInMemoryDatabase("TestDb")
                .Options;

            services.AddScoped(sp => new BotDbContext<BotUser>(options));
            services.AddScoped(sp => new BotDbContext(
                new DbContextOptionsBuilder<BotDbContext>().UseInMemoryDatabase("TestDb").Options));

            // Создаём единый WizardRegistry из визардов App + тестовых визардов.
            // Это нужно потому что BotApplicationBuilder.Build() уже добавил App-регистрацию,
            // но тестовые визарды (TestWizard и т.д.) в неё не входят.
            var appAssembly = typeof(MainMenuScreen).Assembly;
            var testAssembly = typeof(BotWebApplicationFactory).Assembly;

            var combinedWizardRegistry = new WizardRegistry();
            combinedWizardRegistry.RegisterFromAssembly(appAssembly);
            combinedWizardRegistry.RegisterFromAssembly(testAssembly);

            foreach (var wizardType in WizardRegistry.GetWizardTypes(testAssembly))
                services.TryAddScoped(wizardType);

            services.AddSingleton(combinedWizardRegistry);
        });
    }
}