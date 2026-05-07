using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using UpdateContext = TelegramBotFlow.Core.Context.UpdateContext;

namespace TelegramBotFlow.Core.Tests.Pipeline;

public sealed class PrivateChatOnlyMiddlewareTests
{
    private readonly PrivateChatOnlyMiddleware _middleware = new();

    private static UpdateContext CreateMessageContextWithChatType(ChatType chatType)
    {
        var update = new Update
        {
            Message = new Message
            {
                Text = "hello",
                From = new User { Id = 123, FirstName = "Test" },
                Chat = new Chat { Id = 456, Type = chatType },
                Date = DateTime.UtcNow,
                Id = 1
            }
        };
        return new UpdateContext(update, Substitute.For<IServiceProvider>());
    }

    private static UpdateContext CreateChannelPostContext()
    {
        var update = new Update
        {
            ChannelPost = new Message
            {
                Text = "channel post",
                Chat = new Chat { Id = 789, Type = ChatType.Channel },
                Date = DateTime.UtcNow,
                Id = 2
            }
        };
        return new UpdateContext(update, Substitute.For<IServiceProvider>());
    }

    [Fact]
    public async Task Private_chat_message_passes_to_next()
    {
        UpdateContext context = CreateMessageContextWithChatType(ChatType.Private);

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task Group_chat_message_is_blocked()
    {
        UpdateContext context = CreateMessageContextWithChatType(ChatType.Group);

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Channel_post_is_blocked()
    {
        UpdateContext context = CreateChannelPostContext();

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.False(nextCalled);
    }

    [Fact]
    public async Task Supergroup_message_is_blocked()
    {
        UpdateContext context = CreateMessageContextWithChatType(ChatType.Supergroup);

        bool nextCalled = false;
        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.False(nextCalled);
    }
}
