using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Sessions;
using UpdateContext = TelegramBotFlow.Core.Context.UpdateContext;

namespace TelegramBotFlow.Core.Tests.Pipeline;

public sealed class PendingInputMiddlewareTests
{
    private readonly InputHandlerRegistry _registry;
    private readonly PendingInputMiddleware _middleware;

    public PendingInputMiddlewareTests()
    {
        _registry = new InputHandlerRegistry();
        _middleware = new PendingInputMiddleware(_registry, NullLogger<PendingInputMiddleware>.Instance);
    }

    private static UserSession CreateSession(long userId = 123)
    {
        return new UserSession(userId);
    }

    [Fact]
    public async Task Callback_query_passes_to_next_without_checking_pending()
    {
        UpdateContext context = TestHelpers.CreateCallbackContext("some:callback");
        var session = CreateSession();
        session.Navigation.SetPending("some-action");
        context.Session = session;

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        // Pending should remain unchanged — middleware doesn't touch it for callbacks
        Assert.Equal("some-action", context.Session!.Navigation.PendingInputActionId);
    }

    [Fact]
    public async Task Command_message_clears_pending_and_passes_to_next()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("/start");
        var session = CreateSession();
        session.Navigation.SetPending("some-action");
        context.Session = session;

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Null(context.Session!.Navigation.PendingInputActionId);
    }

    [Fact]
    public async Task Text_message_with_registered_handler_invokes_handler_next_not_called()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("some user text");
        var session = CreateSession();
        session.Navigation.SetPending("my-action");
        context.Session = session;

        bool handlerCalled = false;
        bool nextCalled = false;

        _registry.Register("my-action", _ =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(handlerCalled);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Text_message_with_no_pending_passes_to_next()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("some text");
        var session = CreateSession();
        // No pending set
        context.Session = session;

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Text_message_with_pending_but_unregistered_handler_clears_pending_and_passes_to_next()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("some text");
        var session = CreateSession();
        session.Navigation.SetPending("unknown-action");
        context.Session = session;

        // "unknown-action" not registered in registry

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        Assert.Null(context.Session!.Navigation.PendingInputActionId);
    }

    [Fact]
    public async Task PhotoMessage_WithPendingAction_RoutesToHandler()
    {
        bool handlerCalled = false;
        _registry.Register("photo_action", _ => { handlerCalled = true; return Task.CompletedTask; });

        var update = new Update
        {
            Message = new Message
            {
                Photo = [new PhotoSize { FileId = "p1", Width = 100, Height = 100, FileUniqueId = "u1" }],
                From = new User { Id = 123, FirstName = "Test" },
                Chat = new Chat { Id = 456, Type = ChatType.Private },
                Date = DateTime.UtcNow,
                Id = 1
            }
        };
        var ctx = new UpdateContext(update, Substitute.For<IServiceProvider>());
        ctx.Session = CreateSession();
        ctx.Session.Navigation.SetPending("photo_action");

        bool nextCalled = false;
        await _middleware.InvokeAsync(ctx, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(handlerCalled);
        Assert.False(nextCalled);
    }
}
