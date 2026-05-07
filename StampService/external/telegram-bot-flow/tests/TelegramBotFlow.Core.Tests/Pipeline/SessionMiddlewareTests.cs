using Microsoft.Extensions.Options;
using NSubstitute;
using TelegramBotFlow.Core.Hosting;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Sessions;
using UpdateContext = TelegramBotFlow.Core.Context.UpdateContext;

namespace TelegramBotFlow.Core.Tests.Pipeline;

public sealed class SessionMiddlewareTests
{
    private readonly ISessionStore _sessionStore;
    private readonly ISessionLockProvider _lockProvider;
    private readonly IDisposable _sessionLock;
    private readonly SessionMiddleware _middleware;

    public SessionMiddlewareTests()
    {
        _sessionStore = Substitute.For<ISessionStore>();
        _lockProvider = Substitute.For<ISessionLockProvider>();
        _sessionLock = Substitute.For<IDisposable>();

        _lockProvider
            .AcquireLockAsync(Arg.Any<long>(), Arg.Any<CancellationToken>())
            .Returns(_sessionLock);

        var config = Options.Create(new BotConfiguration { Token = "test" });
        _middleware = new SessionMiddleware(_sessionStore, _lockProvider, config);
    }

    [Fact]
    public async Task Session_is_saved_after_pipeline_completes()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("/start", userId: 123);
        var session = new UserSession(123);
        _sessionStore.GetOrCreateAsync(123, Arg.Any<CancellationToken>()).Returns(session);

        await _middleware.InvokeAsync(context, _ => Task.CompletedTask);

        await _sessionStore.Received(1).SaveAsync(session, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Session_is_saved_even_when_pipeline_throws()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("/start", userId: 123);
        var session = new UserSession(123);
        _sessionStore.GetOrCreateAsync(123, Arg.Any<CancellationToken>()).Returns(session);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _middleware.InvokeAsync(context, _ => throw new InvalidOperationException("boom")));

        await _sessionStore.Received(1).SaveAsync(session, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Lock_is_released_after_save()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("/start", userId: 123);
        var session = new UserSession(123);
        _sessionStore.GetOrCreateAsync(123, Arg.Any<CancellationToken>()).Returns(session);

        var callOrder = new List<string>();

        _sessionStore
            .When(s => s.SaveAsync(Arg.Any<UserSession>(), Arg.Any<CancellationToken>()))
            .Do(_ => callOrder.Add("save"));

        _sessionLock
            .When(l => l.Dispose())
            .Do(_ => callOrder.Add("lock_released"));

        await _middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Equal(new[] { "save", "lock_released" }, callOrder);
    }

    [Fact]
    public async Task Session_is_set_on_context_before_next()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("/start", userId: 123);
        var session = new UserSession(123);
        _sessionStore.GetOrCreateAsync(123, Arg.Any<CancellationToken>()).Returns(session);

        UserSession? sessionInNext = null;
        await _middleware.InvokeAsync(context, ctx =>
        {
            sessionInNext = ctx.Session;
            return Task.CompletedTask;
        });

        Assert.Same(session, sessionInNext);
    }

    [Fact]
    public async Task Skips_session_for_zero_userId()
    {
        // Create context with userId=0 by using a channel post update (no From field)
        var update = new Telegram.Bot.Types.Update
        {
            ChannelPost = new Telegram.Bot.Types.Message
            {
                Chat = new Telegram.Bot.Types.Chat
                {
                    Id = 456,
                    Type = Telegram.Bot.Types.Enums.ChatType.Channel
                },
                Date = DateTime.UtcNow,
                Id = 1
            }
        };
        var context = new UpdateContext(update, Substitute.For<IServiceProvider>());
        Assert.Equal(0, context.UserId);

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        await _lockProvider.DidNotReceive().AcquireLockAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _sessionStore.DidNotReceive().GetOrCreateAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _sessionStore.DidNotReceive().SaveAsync(Arg.Any<UserSession>(), Arg.Any<CancellationToken>());
    }
}
