using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using TelegramBotFlow.Core.Messaging;

namespace TelegramBotFlow.Core.Tests.Messaging;

public class ChatAdministrationApiTests
{
    private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
    private readonly IChatAdministrationApi _api;

    public ChatAdministrationApiTests()
    {
        _api = new ChatAdministrationApi(_bot, NullLogger<ChatAdministrationApi>.Instance);
    }

    [Fact]
    public async Task GetChatAsync_Returns_Success_With_Normalized_Type()
    {
        var chat = new ChatFullInfo
        {
            Id = -1001234567,
            Type = Telegram.Bot.Types.Enums.ChatType.Supergroup,
            Title = "My group",
            Username = "my_group",
        };
        _bot.SendRequest(Arg.Any<IRequest<ChatFullInfo>>(), Arg.Any<CancellationToken>())
            .Returns(chat);

        ChatApiResult<ChatInfo> result = await _api.GetChatAsync(-1001234567);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(-1001234567);
        result.Value.Type.Should().Be("supergroup");
        result.Value.Title.Should().Be("My group");
    }

    [Fact]
    public async Task GetChatAsync_Returns_Failure_On_ApiException()
    {
        _bot.SendRequest(Arg.Any<IRequest<ChatFullInfo>>(), Arg.Any<CancellationToken>())
            .Throws(new ApiRequestException("chat not found", 400));

        ChatApiResult<ChatInfo> result = await _api.GetChatAsync(-1);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ChatApiErrorCode.ChatNotReachable);
    }

    [Fact]
    public async Task GetChatMemberAsync_Normalizes_Member_Status()
    {
        var member = new ChatMemberMember
        {
            User = new User { Id = 1001, FirstName = "U", IsBot = false }
        };
        _bot.SendRequest(Arg.Any<IRequest<ChatMember>>(), Arg.Any<CancellationToken>())
            .Returns(member);

        ChatApiResult<ChatMemberInfo> result = await _api.GetChatMemberAsync(-1, 1001);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Membership.Should().Be(ChatMembership.MEMBER);
        result.Value.IsActiveMember.Should().BeTrue();
    }

    [Fact]
    public async Task GetChatMemberAsync_400_Maps_To_NotMember_Not_Failure()
    {
        _bot.SendRequest(Arg.Any<IRequest<ChatMember>>(), Arg.Any<CancellationToken>())
            .Throws(new ApiRequestException("user not found", 400));

        ChatApiResult<ChatMemberInfo> result = await _api.GetChatMemberAsync(-1, 9999);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Membership.Should().Be(ChatMembership.NOT_MEMBER);
        result.Value.IsActiveMember.Should().BeFalse();
    }

    [Fact]
    public async Task GetBotPermissionsAsync_Maps_Administrator_Rights()
    {
        var admin = new ChatMemberAdministrator
        {
            User = new User { Id = 42, FirstName = "Bot", IsBot = true },
            CanInviteUsers = true,
            CanRestrictMembers = true,
            CanManageChat = true,
        };
        _bot.SendRequest(Arg.Any<IRequest<ChatMember>>(), Arg.Any<CancellationToken>())
            .Returns(admin);

        ChatApiResult<BotChatPermissions> result = await _api.GetBotPermissionsAsync(-1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAdministrator.Should().BeTrue();
        result.Value.CanInviteUsers.Should().BeTrue();
        result.Value.CanRestrictMembers.Should().BeTrue();
    }

    [Fact]
    public async Task GetBotPermissionsAsync_NonAdmin_Returns_All_False()
    {
        var member = new ChatMemberMember
        {
            User = new User { Id = 42, FirstName = "Bot", IsBot = true }
        };
        _bot.SendRequest(Arg.Any<IRequest<ChatMember>>(), Arg.Any<CancellationToken>())
            .Returns(member);

        ChatApiResult<BotChatPermissions> result = await _api.GetBotPermissionsAsync(-1);

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAdministrator.Should().BeFalse();
        result.Value.CanInviteUsers.Should().BeFalse();
    }

    [Fact]
    public async Task CreateJoinRequestInviteLinkAsync_Returns_Link_String()
    {
        var link = new ChatInviteLink
        {
            InviteLink = "https://t.me/+abcXYZ",
            CreatesJoinRequest = true,
            Creator = new User { Id = 1, FirstName = "Bot", IsBot = true }
        };
        _bot.SendRequest(Arg.Any<IRequest<ChatInviteLink>>(), Arg.Any<CancellationToken>())
            .Returns(link);

        ChatApiResult<string> result = await _api.CreateJoinRequestInviteLinkAsync(-1, "course_x");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("https://t.me/+abcXYZ");
    }

    [Fact]
    public async Task RevokeChatInviteLinkAsync_Swallows_Errors_For_Best_Effort()
    {
        _bot.SendRequest(Arg.Any<IRequest<ChatInviteLink>>(), Arg.Any<CancellationToken>())
            .Throws(new ApiRequestException("already revoked", 400));

        ChatApiResult<bool> result = await _api.RevokeChatInviteLinkAsync(-1, "https://t.me/+x");

        // Идемпотентный — link мог уже быть удалён, чат тоже.
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveChatJoinRequestAsync_Success()
    {
        _bot.SendRequest(Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        ChatApiResult<bool> result = await _api.ApproveChatJoinRequestAsync(-1, 1001);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveChatJoinRequestAsync_ApiException_Returns_Failure()
    {
        _bot.SendRequest(Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>())
            .Throws(new ApiRequestException("Forbidden", 403));

        ChatApiResult<bool> result = await _api.ApproveChatJoinRequestAsync(-1, 1001);

        result.IsFailure.Should().BeTrue();
        result.ErrorCode.Should().Be(ChatApiErrorCode.ChatNotReachable);
    }

    [Fact]
    public async Task KickChatMemberAsync_Calls_Ban_Then_Unban()
    {
        _bot.SendRequest(Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        ChatApiResult<bool> result = await _api.KickChatMemberAsync(-1, 1001);

        result.IsSuccess.Should().BeTrue();
        // Two API calls: BanChatMember + UnbanChatMember
        await _bot.Received(2).SendRequest(
            Arg.Any<IRequest<bool>>(), Arg.Any<CancellationToken>());
    }
}
