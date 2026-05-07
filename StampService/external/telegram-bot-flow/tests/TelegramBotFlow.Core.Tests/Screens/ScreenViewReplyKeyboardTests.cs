using FluentAssertions;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.UI;

namespace TelegramBotFlow.Core.Tests.Screens;

public sealed class ScreenViewReplyKeyboardTests
{
    [Fact]
    public void WithReplyKeyboard_SetsReplyMarkup()
    {
        var view = new ScreenView("Test")
            .WithReplyKeyboard(kb => kb.RequestContact("Share phone"));

        view.ReplyKeyboard.Should().NotBeNull();
    }

    [Fact]
    public void RemoveReplyKeyboard_SetsRemoveFlag()
    {
        var view = new ScreenView("Test")
            .RemoveReplyKeyboard();

        view.ShouldRemoveReplyKeyboard.Should().BeTrue();
    }

    [Fact]
    public void Default_NoReplyKeyboard()
    {
        var view = new ScreenView("Test");

        view.ReplyKeyboard.Should().BeNull();
        view.ShouldRemoveReplyKeyboard.Should().BeFalse();
    }
}
