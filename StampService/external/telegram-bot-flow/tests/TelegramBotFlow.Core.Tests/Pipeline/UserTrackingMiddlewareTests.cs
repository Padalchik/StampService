using NSubstitute;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using TelegramBotFlow.Core.Users;
using UpdateContext = TelegramBotFlow.Core.Context.UpdateContext;

namespace TelegramBotFlow.Core.Tests.Pipeline;

public sealed class UserTrackingMiddlewareTests
{
    public class TestUser : IBotUser
    {
        public long TelegramId { get; init; }
        public bool IsBlocked { get; set; }
        public DateTime JoinedAt => DateTime.UtcNow;
    }

    [Fact]
    public async Task NewUser_CreatesAndSetsOnContext()
    {
        long userId = 10001;
        IBotUserStore<TestUser> store = Substitute.For<IBotUserStore<TestUser>>();
        store.FindByTelegramIdAsync(userId, Arg.Any<CancellationToken>()).Returns((TestUser?)null);

        var middleware = new UserTrackingMiddleware<TestUser>(store);
        UpdateContext context = TestHelpers.CreateMessageContext("/start", userId: userId);

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.NotNull(context.User);
        Assert.Equal(userId, context.User!.TelegramId);
        await store.Received(1).FindByTelegramIdAsync(userId, Arg.Any<CancellationToken>());
        await store.Received(1).CreateAsync(Arg.Is<TestUser>(u => u.TelegramId == userId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistingUser_SetsOnContextWithoutCreate()
    {
        long userId = 10002;
        var existing = new TestUser { TelegramId = userId };
        IBotUserStore<TestUser> store = Substitute.For<IBotUserStore<TestUser>>();
        store.FindByTelegramIdAsync(userId, Arg.Any<CancellationToken>()).Returns(existing);

        var middleware = new UserTrackingMiddleware<TestUser>(store);
        UpdateContext context = TestHelpers.CreateMessageContext("/start", userId: userId);

        await middleware.InvokeAsync(context, _ => Task.CompletedTask);

        Assert.Same(existing, context.User);
        await store.Received(1).FindByTelegramIdAsync(userId, Arg.Any<CancellationToken>());
        await store.DidNotReceive().CreateAsync(Arg.Any<TestUser>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CachedUser_SkipsStoreLookupOnSecondCall()
    {
        long userId = 10003;
        var existing = new TestUser { TelegramId = userId };
        IBotUserStore<TestUser> store = Substitute.For<IBotUserStore<TestUser>>();
        store.FindByTelegramIdAsync(userId, Arg.Any<CancellationToken>()).Returns(existing);

        var middleware = new UserTrackingMiddleware<TestUser>(store);
        UpdateContext first = TestHelpers.CreateMessageContext("hello", userId: userId);
        UpdateContext second = TestHelpers.CreateMessageContext("hello again", userId: userId);

        await middleware.InvokeAsync(first, _ => Task.CompletedTask);
        await middleware.InvokeAsync(second, _ => Task.CompletedTask);

        // Store should only be hit once — second call is served from cache
        await store.Received(1).FindByTelegramIdAsync(userId, Arg.Any<CancellationToken>());
        Assert.Same(existing, second.User);
    }

    [Fact]
    public async Task InvokeAsync_CallsNext()
    {
        long userId = 10004;
        IBotUserStore<TestUser> store = Substitute.For<IBotUserStore<TestUser>>();
        store.FindByTelegramIdAsync(userId, Arg.Any<CancellationToken>()).Returns((TestUser?)null);

        var middleware = new UserTrackingMiddleware<TestUser>(store);
        UpdateContext context = TestHelpers.CreateMessageContext("/start", userId: userId);

        bool nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
    }
}
