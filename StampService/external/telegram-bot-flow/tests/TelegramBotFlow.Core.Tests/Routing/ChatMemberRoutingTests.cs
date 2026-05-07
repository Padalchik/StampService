using FluentAssertions;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline;
using TelegramBotFlow.Core.Routing;
using NSubstitute;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramBotFlow.Core.Tests.Routing;

public sealed class ChatMemberRoutingTests
{
    private static readonly UpdateDelegate NoOp = _ => Task.CompletedTask;

    [Fact]
    public void ChatMember_MatchesMyChatMemberUpdate()
    {
        var route = RouteEntry.ChatMember(NoOp);
        var ctx = CreateMyChatMemberContext(ChatMemberStatus.Kicked);

        route.Matches(ctx).Should().BeTrue();
    }

    [Fact]
    public void ChatMember_DoesNotMatchMessageUpdate()
    {
        var route = RouteEntry.ChatMember(NoOp);
        var ctx = TestHelpers.CreateMessageContext("hello");

        route.Matches(ctx).Should().BeFalse();
    }

    [Fact]
    public void ChatMember_DoesNotMatchCallbackUpdate()
    {
        var route = RouteEntry.ChatMember(NoOp);
        var ctx = TestHelpers.CreateCallbackContext("some_callback");

        route.Matches(ctx).Should().BeFalse();
    }

    [Fact]
    public void ChatMember_HasNormalPriority()
    {
        var route = RouteEntry.ChatMember(NoOp);

        route.Priority.Should().Be(RoutePriority.NORMAL);
    }

    [Fact]
    public void ChatMember_HasCorrectRouteType()
    {
        var route = RouteEntry.ChatMember(NoOp);

        route.Type.Should().Be(RouteType.CHAT_MEMBER);
    }

    private static UpdateContext CreateMyChatMemberContext(
        ChatMemberStatus newStatus,
        long userId = 123)
    {
        var update = new Update
        {
            MyChatMember = new ChatMemberUpdated
            {
                Chat = new Chat { Id = userId, Type = ChatType.Private },
                From = new User { Id = userId, FirstName = "Test" },
                Date = DateTime.UtcNow,
                OldChatMember = new ChatMemberMember { User = new User { Id = userId, FirstName = "Test" } },
                NewChatMember = newStatus switch
                {
                    ChatMemberStatus.Kicked => new ChatMemberBanned
                    {
                        User = new User { Id = userId, FirstName = "Test" }
                    },
                    ChatMemberStatus.Member => new ChatMemberMember
                    {
                        User = new User { Id = userId, FirstName = "Test" }
                    },
                    _ => new ChatMemberMember
                    {
                        User = new User { Id = userId, FirstName = "Test" }
                    }
                }
            }
        };

        return new UpdateContext(update, Substitute.For<IServiceProvider>());
    }
}
