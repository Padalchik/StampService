using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TelegramBotFlow.Core;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Pipeline.Middlewares;
using UpdateContext = TelegramBotFlow.Core.Context.UpdateContext;

namespace TelegramBotFlow.Core.Tests.Pipeline;

public sealed class ErrorHandlingMiddlewareTests
{
    private readonly IUpdateResponder _responder;
    private readonly ErrorHandlingMiddleware _middleware;

    public ErrorHandlingMiddlewareTests()
    {
        _responder = Substitute.For<IUpdateResponder>();

        var messages = new BotMessages();
        IOptions<BotMessages> options = Options.Create(messages);
        ILogger<ErrorHandlingMiddleware> logger = NullLogger<ErrorHandlingMiddleware>.Instance;

        _middleware = new ErrorHandlingMiddleware(logger, _responder, options);
    }

    [Fact]
    public async Task Happy_path_next_called_no_error_message_sent()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("/start");
        bool nextCalled = false;

        await _middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
        await _responder.DidNotReceive().ReplyAsync(
            Arg.Any<UpdateContext>(),
            Arg.Any<string>(),
            Arg.Any<Telegram.Bot.Types.ReplyMarkups.ReplyMarkup?>(),
            Arg.Any<Telegram.Bot.Types.Enums.ParseMode>());
    }

    [Fact]
    public async Task Pipeline_throws_error_message_sent_and_exception_rethrown()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("/start");
        var boom = new InvalidOperationException("boom");

        _responder
            .ReplyAsync(Arg.Any<UpdateContext>(), Arg.Any<string>(),
                Arg.Any<Telegram.Bot.Types.ReplyMarkups.ReplyMarkup?>(),
                Arg.Any<Telegram.Bot.Types.Enums.ParseMode>())
            .Returns(new Telegram.Bot.Types.Message());

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _middleware.InvokeAsync(context, _ => throw boom));

        Assert.Same(boom, thrown);
        await _responder.Received(1).ReplyAsync(
            context,
            "An error occurred. Please try again later.",
            Arg.Any<Telegram.Bot.Types.ReplyMarkups.ReplyMarkup?>(),
            Arg.Any<Telegram.Bot.Types.Enums.ParseMode>());
    }

    [Fact]
    public async Task OperationCanceledException_when_token_cancelled_is_swallowed_no_error_sent()
    {
        using var cts = new CancellationTokenSource();
        var update = new Telegram.Bot.Types.Update
        {
            Message = new Telegram.Bot.Types.Message
            {
                Text = "/start",
                From = new Telegram.Bot.Types.User { Id = 123, FirstName = "Test" },
                Chat = new Telegram.Bot.Types.Chat { Id = 456, Type = Telegram.Bot.Types.Enums.ChatType.Private },
                Date = DateTime.UtcNow,
                Id = 1
            }
        };
        var context = new UpdateContext(update, Substitute.For<IServiceProvider>(), cts.Token);
        cts.Cancel();

        // Should not throw
        await _middleware.InvokeAsync(context, _ => throw new OperationCanceledException(cts.Token));

        await _responder.DidNotReceive().ReplyAsync(
            Arg.Any<UpdateContext>(),
            Arg.Any<string>(),
            Arg.Any<Telegram.Bot.Types.ReplyMarkups.ReplyMarkup?>(),
            Arg.Any<Telegram.Bot.Types.Enums.ParseMode>());
    }

    [Fact]
    public async Task TryNotifyUser_fails_original_exception_still_rethrown()
    {
        UpdateContext context = TestHelpers.CreateMessageContext("/start");
        var originalException = new InvalidOperationException("original");

        _responder
            .ReplyAsync(Arg.Any<UpdateContext>(), Arg.Any<string>(),
                Arg.Any<Telegram.Bot.Types.ReplyMarkups.ReplyMarkup?>(),
                Arg.Any<Telegram.Bot.Types.Enums.ParseMode>())
            .ThrowsAsync(new Exception("notification failed"));

        InvalidOperationException thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _middleware.InvokeAsync(context, _ => throw originalException));

        Assert.Same(originalException, thrown);
    }
}
