using FluentAssertions;
using TelegramBotFlow.Core.Users;

namespace TelegramBotFlow.Core.Tests.Context;

public sealed class UpdateContextPropertiesTests
{
    [Fact]
    public void User_DefaultsToNull()
    {
        var ctx = TestHelpers.CreateMessageContext("hello");
        ctx.User.Should().BeNull();
    }

    [Fact]
    public void HandlerName_DefaultsToNull()
    {
        var ctx = TestHelpers.CreateMessageContext("hello");
        ctx.HandlerName.Should().BeNull();
    }
}
