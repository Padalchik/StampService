using FluentAssertions;
using NSubstitute;
using Telegram.Bot;
using Telegram.Bot.Requests.Abstractions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBotFlow.Core.Messaging;

namespace TelegramBotFlow.Core.Tests.Messaging;

public class BotNotifierTests
{
    private readonly ITelegramBotClient _bot = Substitute.For<ITelegramBotClient>();
    private readonly IBotNotifier _notifier;

    public BotNotifierTests()
    {
        _notifier = new BotNotifier(_bot);
    }

    [Fact]
    public async Task SendTextAsync_CallsSendRequest_WithSendMessageRequest()
    {
        var expected = new Message { Id = 1, Chat = new Chat { Id = 100 } };
        _bot.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        Message result = await _notifier.SendTextAsync(100, "Hello");

        result.Should().Be(expected);
        await _bot.Received(1).SendRequest(
            Arg.Is<IRequest<Message>>(r => r.GetType().Name.Contains("SendMessage")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendTextAsync_UsesHtmlParseMode_ByDefault()
    {
        _bot.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>())
            .Returns(new Message { Id = 1, Chat = new Chat { Id = 100 } });

        await _notifier.SendTextAsync(100, "text");

        await _bot.Received(1).SendRequest(
            Arg.Is<IRequest<Message>>(r => r.GetType().Name.Contains("SendMessage")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendPhotoAsync_CallsSendRequest_WithSendPhotoRequest()
    {
        var expected = new Message { Id = 2, Chat = new Chat { Id = 200 } };
        _bot.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var photo = InputFile.FromFileId("photo-id");

        Message result = await _notifier.SendPhotoAsync(200, photo, "Caption");

        result.Should().Be(expected);
        await _bot.Received(1).SendRequest(
            Arg.Is<IRequest<Message>>(r => r.GetType().Name.Contains("SendPhoto")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendDocumentAsync_CallsSendRequest_WithSendDocumentRequest()
    {
        var expected = new Message { Id = 3, Chat = new Chat { Id = 300 } };
        _bot.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var doc = InputFile.FromFileId("doc-id");

        Message result = await _notifier.SendDocumentAsync(300, doc, "My Doc");

        result.Should().Be(expected);
        await _bot.Received(1).SendRequest(
            Arg.Is<IRequest<Message>>(r => r.GetType().Name.Contains("SendDocument")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CopyMessageAsync_CallsSendRequest_AndReturnsMessageWithCorrectId()
    {
        _bot.SendRequest(Arg.Any<IRequest<MessageId>>(), Arg.Any<CancellationToken>())
            .Returns(new MessageId { Id = 42 });

        Message result = await _notifier.CopyMessageAsync(100, 200, 5);

        result.Id.Should().Be(42);
        result.Chat.Id.Should().Be(100);
        await _bot.Received(1).SendRequest(
            Arg.Any<IRequest<MessageId>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendTextAsync_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        _bot.SendRequest(Arg.Any<IRequest<Message>>(), Arg.Any<CancellationToken>())
            .Returns(new Message { Id = 1, Chat = new Chat { Id = 1 } });

        await _notifier.SendTextAsync(1, "hi", ct: cts.Token);

        await _bot.Received(1).SendRequest(
            Arg.Any<IRequest<Message>>(),
            cts.Token);
    }
}
