using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.IntegrationTests.Infrastructure;

namespace TelegramBotFlow.IntegrationTests.Application;

[Collection(nameof(BotApplicationTests))]
public class ApplicationStartupTests : IClassFixture<BotWebApplicationFactory>
{
    private readonly BotWebApplicationFactory _factory;

    public ApplicationStartupTests(BotWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Application_Should_Start_Successfully()
    {
        HttpClient client = _factory.CreateClient();
        IServiceProvider services = _factory.Services;

        client.Should().NotBeNull();
        services.Should().NotBeNull();
    }

    [Fact]
    public void Should_Resolve_TelegramBotClient()
    {
        ITelegramBotClient? botClient = _factory.Services.GetService<ITelegramBotClient>();

        botClient.Should().NotBeNull("ITelegramBotClient stub должен быть зарегистрирован");
    }

    [Fact]
    public void Should_Resolve_Core_Singleton_Services()
    {
        UpdateRouter? updateRouter = _factory.Services.GetService<UpdateRouter>();
        ISessionStore? sessionStore = _factory.Services.GetService<ISessionStore>();
        UpdatePipeline? updatePipeline = _factory.Services.GetService<UpdatePipeline>();

        updateRouter.Should().NotBeNull("UpdateRouter should be registered");
        sessionStore.Should().NotBeNull("ISessionStore should be registered");
        updatePipeline.Should().NotBeNull("UpdatePipeline should be registered");
    }

    [Fact]
    public void Should_Resolve_All_Middlewares_As_Scoped()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IServiceProvider scopedServices = scope.ServiceProvider;

        ErrorHandlingMiddleware? errorHandling = scopedServices.GetService<ErrorHandlingMiddleware>();
        LoggingMiddleware? logging = scopedServices.GetService<LoggingMiddleware>();
        SessionMiddleware? session = scopedServices.GetService<SessionMiddleware>();

        errorHandling.Should().NotBeNull("ErrorHandlingMiddleware should be registered");
        logging.Should().NotBeNull("LoggingMiddleware should be registered");
        session.Should().NotBeNull("SessionMiddleware should be registered");
    }

    [Fact]
    public void Singleton_Services_Should_Return_Same_Instance()
    {
        ITelegramBotClient bot1 = _factory.Services.GetRequiredService<ITelegramBotClient>();
        ITelegramBotClient bot2 = _factory.Services.GetRequiredService<ITelegramBotClient>();

        UpdateRouter router1 = _factory.Services.GetRequiredService<UpdateRouter>();
        UpdateRouter router2 = _factory.Services.GetRequiredService<UpdateRouter>();

        bot1.Should().BeSameAs(bot2, "singleton should return same instance");
        router1.Should().BeSameAs(router2, "singleton should return same instance");
    }

    [Fact]
    public void Scoped_Services_Should_Return_Different_Instances_Per_Scope()
    {
        using IServiceScope scope1 = _factory.Services.CreateScope();
        using IServiceScope scope2 = _factory.Services.CreateScope();

        ErrorHandlingMiddleware middleware1 = scope1.ServiceProvider.GetRequiredService<ErrorHandlingMiddleware>();
        ErrorHandlingMiddleware middleware2 = scope2.ServiceProvider.GetRequiredService<ErrorHandlingMiddleware>();

        middleware1.Should().NotBeSameAs(middleware2,
           "scoped services should return different instances in different scopes");
    }

    [Fact]
    public void Should_Create_Multiple_Scopes_Without_Errors()
    {
        Action action = () =>
        {
            for (int i = 0; i < 10; i++)
            {
                using IServiceScope scope = _factory.Services.CreateScope();
                ErrorHandlingMiddleware errorHandling =
                    scope.ServiceProvider.GetRequiredService<ErrorHandlingMiddleware>();
                errorHandling.Should().NotBeNull();
            }
        };

        action.Should().NotThrow("creating multiple scopes should not cause errors");
    }

    [Fact]
    public void Should_Have_Configuration_Loaded()
    {
        Microsoft.Extensions.Configuration.IConfiguration? configuration =
            _factory.Services.GetService<Microsoft.Extensions.Configuration.IConfiguration>();

        configuration.Should().NotBeNull();
        configuration["Bot:Token"].Should().Be("fake-token-for-testing");
        configuration["Bot:Mode"].Should().Be("Polling");
    }

    [Fact]
    public void MockResponder_Should_Be_Registered_And_Resolvable()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IUpdateResponder? responder = scope.ServiceProvider.GetService<IUpdateResponder>();

        responder.Should().NotBeNull("IUpdateResponder должен быть зарегистрирован");
        responder.Should().BeSameAs(_factory.MockResponder,
            "в тестах должен использоваться мок IUpdateResponder");
    }
}