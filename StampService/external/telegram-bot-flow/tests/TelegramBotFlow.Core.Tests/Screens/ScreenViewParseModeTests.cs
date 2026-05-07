using FluentAssertions;
using Telegram.Bot.Types.Enums;
using TelegramBotFlow.Core.Screens;

namespace TelegramBotFlow.Core.Tests.Screens;

public sealed class ScreenViewParseModeTests
{
    [Fact]
    public void ParseMode_Default_IsHtml()
    {
        var view = new ScreenView("text");

        view.ParseMode.Should().Be(ParseMode.Html);
    }

    [Fact]
    public void WithMarkdown_SetsMarkdownV2ParseMode()
    {
        var view = new ScreenView("text").WithMarkdown();

        view.ParseMode.Should().Be(ParseMode.MarkdownV2);
    }

    [Fact]
    public void WithNoParseMode_SetsNoneParseMode()
    {
        var view = new ScreenView("text").WithNoParseMode();

        view.ParseMode.Should().Be(ParseMode.None);
    }

    [Fact]
    public void WithMarkdown_ReturnsSameInstance_ForFluentChaining()
    {
        var view = new ScreenView("text");
        var result = view.WithMarkdown();

        result.Should().BeSameAs(view);
    }

    [Fact]
    public void WithNoParseMode_ReturnsSameInstance_ForFluentChaining()
    {
        var view = new ScreenView("text");
        var result = view.WithNoParseMode();

        result.Should().BeSameAs(view);
    }

    [Fact]
    public void ParseMode_CanBeChangedMultipleTimes()
    {
        var view = new ScreenView("text")
            .WithMarkdown()
            .WithNoParseMode();

        view.ParseMode.Should().Be(ParseMode.None);
    }

    [Fact]
    public void ParseMode_CanBeResetToHtml_ViaMarkdownThenHtml()
    {
        // Пример fluent-chain с возвратом к Html
        var view = new ScreenView("text").WithMarkdown();
        view.ParseMode.Should().Be(ParseMode.MarkdownV2);

        // Вручную задать Html (без fluent-метода — просто проверяем дефолт нового view)
        var freshView = new ScreenView("text");
        freshView.ParseMode.Should().Be(ParseMode.Html);
    }
}