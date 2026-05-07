using FluentAssertions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Routing;

file record SamplePayload(int Id, string Name);

public sealed class PayloadDelegateFactoryTests
{
    // -- Helpers --

    private static UpdateContext CreateCallbackContext(
        string callbackData,
        IServiceProvider? services = null,
        UserSession? session = null)
    {
        var update = new Update
        {
            CallbackQuery = new CallbackQuery
            {
                Id = "cb-1",
                Data = callbackData,
                From = new User { Id = 1, FirstName = "T" },
                Message = new Message
                {
                    Id = 10,
                    Date = DateTime.UtcNow,
                    Chat = new Chat { Id = 1, Type = ChatType.Private },
                    From = new User { Id = 999, IsBot = true, FirstName = "Bot" }
                }
            }
        };

        IServiceProvider sp = services ?? BuildServices();
        var ctx = new UpdateContext(update, sp);
        ctx.Session = session ?? new UserSession(1);
        return ctx;
    }

    private static IServiceProvider BuildServices(INavigationService? navigator = null,
        IUpdateResponder? responder = null)
    {
        IServiceProvider services = Substitute.For<IServiceProvider>();
        INavigationService nav = navigator ?? Substitute.For<INavigationService>();
        IUpdateResponder resp = responder ?? Substitute.For<IUpdateResponder>();
        services.GetService(typeof(INavigationService)).Returns(nav);
        services.GetService(typeof(IUpdateResponder)).Returns(resp);
        return services;
    }

    // -- JSON payload --

    [Fact]
    public async Task Create_DeserializesJsonPayload_AndPassesToHandler()
    {
        string? captured = null;

        Delegate handler = (string payload) =>
        {
            captured = payload;
            return Task.FromResult(BotResults.Empty());
        };

        UpdateDelegate del = PayloadDelegateFactory.Create<string>(handler, "MyAction");
        UpdateContext ctx = CreateCallbackContext("MyAction:j:\"hello\"");

        await del(ctx);

        captured.Should().Be("hello");
    }

    [Fact]
    public async Task Create_DeserializesComplexJsonPayload()
    {
        SamplePayload? captured = null;

        Delegate handler = (SamplePayload payload) =>
        {
            captured = payload;
            return Task.FromResult(BotResults.Empty());
        };

        UpdateDelegate del = PayloadDelegateFactory.Create<SamplePayload>(handler, "MyAction");
        UpdateContext ctx = CreateCallbackContext("MyAction:j:{\"Id\":42,\"Name\":\"test\"}");

        await del(ctx);

        captured.Should().NotBeNull();
        captured!.Id.Should().Be(42);
        captured.Name.Should().Be("test");
    }

    // -- Short-ID payload --

    [Fact]
    public async Task Create_ResolvesShortIdPayload_FromSession()
    {
        string? captured = null;
        var session = new UserSession(1);

        // Сохраняем payload с коротким ID в сессию (внутренний метод, доступен через InternalsVisibleTo)
        const string shortId = "test01";
        session.Navigation.StorePayloadJson(shortId, "\"session-value\"");

        Delegate handler = (string payload) =>
        {
            captured = payload;
            return Task.FromResult(BotResults.Empty());
        };

        UpdateDelegate del = PayloadDelegateFactory.Create<string>(handler, "MyAction");
        UpdateContext ctx = CreateCallbackContext($"MyAction:s:{shortId}", session: session);

        await del(ctx);

        captured.Should().Be("session-value");
    }

    // -- PayloadExpiredException --

    [Fact]
    public async Task Create_AnswersCallbackWithAlert_WhenSessionIsNull()
    {
        var responder = Substitute.For<IUpdateResponder>();
        IServiceProvider services = BuildServices(responder: responder);

        Delegate handler = (string payload) => Task.FromResult(BotResults.Empty());

        UpdateDelegate del = PayloadDelegateFactory.Create<string>(handler, "MyAction");
        UpdateContext ctx = CreateCallbackContext("MyAction:s:nonexistent", services: services);
        ctx.Session = null;

        await del(ctx);

        await responder.Received(1).AnswerCallbackAsync(
            Arg.Any<UpdateContext>(),
            Arg.Any<string>(),
            showAlert: true);
    }

    // -- Invalid payload format --

    [Fact]
    public async Task Create_Throws_WhenPayloadFormatIsInvalid()
    {
        Delegate handler = (string payload) => Task.FromResult(BotResults.Empty());

        UpdateDelegate del = PayloadDelegateFactory.Create<string>(handler, "MyAction");
        UpdateContext ctx = CreateCallbackContext("MyAction:INVALID_DATA");

        Func<Task> act = () => del(ctx);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid payload format*");
    }

    // -- DI injection --

    [Fact]
    public async Task Create_InjectsServicesIntoHandler()
    {
        INavigationService? captured = null;
        var nav = Substitute.For<INavigationService>();
        IServiceProvider services = BuildServices(navigator: nav);

        Delegate handler = (string payload, INavigationService svc) =>
        {
            captured = svc;
            return Task.FromResult(BotResults.Empty());
        };

        UpdateDelegate del = PayloadDelegateFactory.Create<string>(handler, "MyAction");
        UpdateContext ctx = CreateCallbackContext("MyAction:j:\"x\"", services: services);

        await del(ctx);

        captured.Should().BeSameAs(nav);
    }

    // -- Handler return type validation --

    [Fact]
    public void Create_Throws_WhenHandlerDoesNotReturnTaskEndpointResult()
    {
        Delegate handler = (string payload) => { };

        Action act = () => PayloadDelegateFactory.Create<string>(handler, "MyAction");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Task<IEndpointResult>*");
    }

    // -- UpdateContext and CancellationToken injection --

    [Fact]
    public async Task Create_InjectsUpdateContextAndCancellationToken()
    {
        UpdateContext? capturedCtx = null;
        CancellationToken capturedCt = default;

        Delegate handler = (UpdateContext ctx, string payload, CancellationToken ct) =>
        {
            capturedCtx = ctx;
            capturedCt = ct;
            return Task.FromResult(BotResults.Empty());
        };

        UpdateDelegate del = PayloadDelegateFactory.Create<string>(handler, "MyAction");
        UpdateContext ctx = CreateCallbackContext("MyAction:j:\"val\"");

        await del(ctx);

        capturedCtx.Should().BeSameAs(ctx);
    }
}