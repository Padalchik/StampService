using FluentAssertions;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Tests.UI;

public sealed class InlineKeyboardTests
{
    [Fact]
    public void Build_SingleRow_CreatesCorrectMarkup()
    {
        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup markup = new InlineKeyboard()
            .Button("Yes", "yes")
            .Button("No", "no")
            .Build();

        markup.InlineKeyboard.Should().HaveCount(1);
        markup.InlineKeyboard.First().Should().HaveCount(2);
    }

    [Fact]
    public void Build_MultipleRows_CreatesCorrectMarkup()
    {
        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup markup = new InlineKeyboard()
            .Button("A", "a")
            .Row()
            .Button("B", "b")
            .Build();

        markup.InlineKeyboard.Should().HaveCount(2);
        markup.InlineKeyboard.First().Should().HaveCount(1);
        markup.InlineKeyboard.Last().Should().HaveCount(1);
    }

    [Fact]
    public void Build_ButtonData_IsCorrect()
    {
        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup markup = new InlineKeyboard()
            .Button("Click", "data123")
            .Build();

        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton button = markup.InlineKeyboard.First().First();
        button.Text.Should().Be("Click");
        button.CallbackData.Should().Be("data123");
    }

    [Fact]
    public void Build_UrlButton_HasCorrectUrl()
    {
        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup markup = new InlineKeyboard()
            .Url("Visit", "https://example.com")
            .Build();

        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton button = markup.InlineKeyboard.First().First();
        button.Text.Should().Be("Visit");
        button.Url.Should().Be("https://example.com");
    }

    [Fact]
    public void SingleButton_CreatesOneButtonMarkup()
    {
        Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup markup = InlineKeyboard.SingleButton("OK", "ok");

        markup.InlineKeyboard.Should().HaveCount(1);
        markup.InlineKeyboard.First().Should().HaveCount(1);
    }
}