using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.Messaging;

namespace TelegramBotFlow.Core.Tests.Messaging;

public class BotBroadcasterTests
{
    private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
    private readonly IBotBroadcaster _broadcaster;

    public BotBroadcasterTests()
    {
        _broadcaster = new BotBroadcaster(_bot);
    }

    [Fact]
    public async Task BroadcastAsync_SendsToAllUsers_ReturnsCorrectCount()
    {
        SetupSendRequestReturnsOk();

        List<long> chatIds = [100, 200, 300];

        BroadcastResult result = await _broadcaster.BroadcastAsync(
            chatIds,
            chatId => new BotMessage($"Hello {chatId}"));

        result.TotalSent.Should().Be(3);
        result.TotalFailed.Should().Be(0);
        result.BlockedUserIds.Should().BeEmpty();
        result.FailedChatIds.Should().BeEmpty();
    }

    [Fact]
    public async Task BroadcastAsync_MessageFactoryReceivesCorrectChatId()
    {
        SetupSendRequestReturnsOk();

        List<long> receivedIds = [];
        List<long> chatIds = [10, 20, 30];

        await _broadcaster.BroadcastAsync(
            chatIds,
            chatId =>
            {
                lock (receivedIds) receivedIds.Add(chatId);
                return new BotMessage("text");
            });

        receivedIds.Should().BeEquivalentTo(chatIds);
    }

    [Fact]
    public async Task BroadcastAsync_Handles403AsBlockedUser()
    {
        _bot.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiRequestException("Forbidden: bot was blocked by the user", 403));

        BroadcastResult result = await _broadcaster.BroadcastAsync(
            [100, 200],
            _ => new BotMessage("Hello"));

        result.TotalSent.Should().Be(0);
        result.TotalFailed.Should().Be(2);
        result.BlockedUserIds.Should().BeEquivalentTo([100, 200]);
        result.FailedChatIds.Should().BeEmpty();
    }

    [Fact]
    public async Task BroadcastAsync_HandlesOtherExceptionsAsFailedChats()
    {
        _bot.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiRequestException("Bad Request", 400));

        BroadcastResult result = await _broadcaster.BroadcastAsync(
            [100],
            _ => new BotMessage("Hello"));

        result.TotalSent.Should().Be(0);
        result.TotalFailed.Should().Be(1);
        result.BlockedUserIds.Should().BeEmpty();
        result.FailedChatIds.Should().ContainSingle().Which.Should().Be(100);
    }

    [Fact]
    public async Task BroadcastAsync_CallsOnErrorCallback()
    {
        _bot.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiRequestException("Forbidden", 403));

        List<(long ChatId, Exception Ex)> errors = [];

        await _broadcaster.BroadcastAsync(
            [500],
            _ => new BotMessage("text"),
            new BroadcastOptions
            {
                OnError = (chatId, ex) =>
                {
                    lock (errors) errors.Add((chatId, ex));
                }
            });

        errors.Should().ContainSingle();
        errors[0].ChatId.Should().Be(500);
        errors[0].Ex.Should().BeOfType<ApiRequestException>();
    }

    [Fact]
    public async Task BroadcastAsync_SendsPhotoWhenPhotoIsSet()
    {
        SetupSendRequestReturnsOk();

        var photo = InputFile.FromFileId("photo-id");

        BroadcastResult result = await _broadcaster.BroadcastAsync(
            [100],
            _ => new BotMessage("caption", Photo: photo));

        result.TotalSent.Should().Be(1);
        // Verify SendPhoto was called (not SendMessage) by checking the request type
        await _bot.Received(1).SendRequest(
            Arg.Is<IRequest<Message>>(r => r.GetType().Name.Contains("SendPhoto")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BroadcastAsync_SendsTextWhenNoPhoto()
    {
        SetupSendRequestReturnsOk();

        BroadcastResult result = await _broadcaster.BroadcastAsync(
            [100],
            _ => new BotMessage("Hello"));

        result.TotalSent.Should().Be(1);
        await _bot.Received(1).SendRequest(
            Arg.Is<IRequest<Message>>(r => r.GetType().Name.Contains("SendMessage")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BroadcastAsync_RespectsMaxConcurrency()
    {
        int maxConcurrent = 0;
        int current = 0;

        _bot.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>())
            .Returns(async ci =>
            {
                int c = Interlocked.Increment(ref current);
                InterlockedMax(ref maxConcurrent, c);
                await Task.Delay(50, ci.ArgAt<CancellationToken>(1));
                Interlocked.Decrement(ref current);
                return new Message { Id = 1, Chat = new Chat { Id = 1 } };
            });

        List<long> chatIds = Enumerable.Range(1, 20).Select(i => (long)i).ToList();

        await _broadcaster.BroadcastAsync(
            chatIds,
            _ => new BotMessage("text"),
            new BroadcastOptions { MaxConcurrency = 3 });

        maxConcurrent.Should().BeLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task BroadcastAsync_403WithMarkBlockedUsersFalse_GoesToFailedChats()
    {
        _bot.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ApiRequestException("Forbidden", 403));

        BroadcastResult result = await _broadcaster.BroadcastAsync(
            [100],
            _ => new BotMessage("text"),
            new BroadcastOptions { MarkBlockedUsers = false });

        result.BlockedUserIds.Should().BeEmpty();
        result.FailedChatIds.Should().ContainSingle().Which.Should().Be(100);
    }

    [Fact]
    public async Task BroadcastAsync_EmptyChatIds_ReturnsZeroCounts()
    {
        BroadcastResult result = await _broadcaster.BroadcastAsync(
            [],
            _ => new BotMessage("text"));

        result.TotalSent.Should().Be(0);
        result.TotalFailed.Should().Be(0);
        result.BlockedUserIds.Should().BeEmpty();
        result.FailedChatIds.Should().BeEmpty();
    }

    [Fact]
    public async Task BroadcastAsync_MixedSuccessAndFailure_ReportsCorrectly()
    {
        int callCount = 0;
        _bot.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                int n = Interlocked.Increment(ref callCount);
                if (n % 2 == 0)
                    throw new ApiRequestException("Forbidden", 403);
                return new Message { Id = n, Chat = new Chat { Id = 1 } };
            });

        BroadcastResult result = await _broadcaster.BroadcastAsync(
            [1, 2, 3, 4],
            _ => new BotMessage("text"),
            new BroadcastOptions { MaxConcurrency = 1 });

        result.TotalSent.Should().Be(2);
        result.TotalFailed.Should().Be(2);
    }

    private void SetupSendRequestReturnsOk()
    {
        _bot.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>())
            .Returns(new Message { Id = 1, Chat = new Chat { Id = 1 } });
    }

    private static void InterlockedMax(ref int location, int value)
    {
        int current;
        do
        {
            current = location;
            if (value <= current) return;
        } while (Interlocked.CompareExchange(ref location, value, current) != current);
    }
}
