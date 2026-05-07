using FluentAssertions;

namespace TelegramBotFlow.Core.Tests;

public sealed class BotMessagesTests
{
    [Fact]
    public void Defaults_AreNotEmpty()
    {
        var messages = new BotMessages();

        messages.BackButton.Should().NotBeNullOrWhiteSpace();
        messages.MenuButton.Should().NotBeNullOrWhiteSpace();
        messages.CloseButton.Should().NotBeNullOrWhiteSpace();
        messages.PayloadExpired.Should().NotBeNullOrWhiteSpace();
        messages.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Properties_AreOverridable()
    {
        var messages = new BotMessages
        {
            BackButton = "Custom Back",
            ErrorMessage = "Custom Error"
        };

        messages.BackButton.Should().Be("Custom Back");
        messages.ErrorMessage.Should().Be("Custom Error");
    }
}
