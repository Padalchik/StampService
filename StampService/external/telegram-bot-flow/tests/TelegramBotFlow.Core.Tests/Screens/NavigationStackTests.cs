using FluentAssertions;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Screens;

public sealed class NavigationStackTests
{
    [Fact]
    public void PushScreen_FirstScreen_SetsCurrentScreen()
    {
        var session = new UserSession(1);

        session.Navigation.PushScreen("main");

        session.Navigation.CurrentScreen.Should().Be("main");
        session.Navigation.NavigationStack.Should().BeEmpty();
    }

    [Fact]
    public void PushScreen_SecondScreen_PushesFirstToStack()
    {
        var session = new UserSession(1);
        session.Navigation.PushScreen("main");

        session.Navigation.PushScreen("settings");

        session.Navigation.CurrentScreen.Should().Be("settings");
        session.Navigation.NavigationStack.Should().ContainSingle().Which.Should().Be("main");
    }

    [Fact]
    public void PopScreen_ReturnsToLastScreen()
    {
        var session = new UserSession(1);
        session.Navigation.PushScreen("main");
        session.Navigation.PushScreen("settings");
        session.Navigation.PushScreen("lang");

        string? popped = session.Navigation.PopScreen();

        popped.Should().Be("settings");
        session.Navigation.CurrentScreen.Should().Be("settings");
        session.Navigation.NavigationStack.Should().ContainSingle().Which.Should().Be("main");
    }

    [Fact]
    public void PopScreen_EmptyStack_ReturnsNull()
    {
        var session = new UserSession(1);
        session.Navigation.PushScreen("main");

        string? popped = session.Navigation.PopScreen();

        popped.Should().BeNull();
        session.Navigation.CurrentScreen.Should().Be("main");
    }

    [Fact]
    public void PopScreen_WithoutPush_ReturnsNull()
    {
        var session = new UserSession(1);

        string? popped = session.Navigation.PopScreen();

        popped.Should().BeNull();
    }

    [Fact]
    public void Clear_ResetsNavigationStack()
    {
        var session = new UserSession(1);
        session.Navigation.PushScreen("main");
        session.Navigation.PushScreen("settings");
        session.Navigation.PushScreen("lang");

        session.Clear();

        session.Navigation.CurrentScreen.Should().BeNull();
        session.Navigation.NavigationStack.Should().BeEmpty();
        session.Navigation.NavMessageId.Should().BeNull();
    }

    [Fact]
    public void DeepNavigation_And_BackToRoot()
    {
        var session = new UserSession(1);
        session.Navigation.PushScreen("main");
        session.Navigation.PushScreen("settings");
        session.Navigation.PushScreen("lang");
        session.Navigation.PushScreen("lang_confirm");

        session.Navigation.PopScreen().Should().Be("lang");
        session.Navigation.PopScreen().Should().Be("settings");
        session.Navigation.PopScreen().Should().Be("main");
        session.Navigation.PopScreen().Should().BeNull();
        session.Navigation.CurrentScreen.Should().Be("main");
    }

    [Fact]
    public void PushScreen_DuplicateInStack_TruncatesToExisting()
    {
        var session = new UserSession(1);
        session.Navigation.PushScreen("main");
        session.Navigation.PushScreen("profile");
        session.Navigation.PushScreen("settings");

        session.Navigation.PushScreen("profile");

        session.Navigation.CurrentScreen.Should().Be("profile");
        session.Navigation.NavigationStack.Should().Equal("main");
    }

    [Fact]
    public void PushScreen_SameAsCurrent_NoOp()
    {
        var session = new UserSession(1);
        session.Navigation.PushScreen("main");
        session.Navigation.PushScreen("settings");

        session.Navigation.PushScreen("settings");

        session.Navigation.CurrentScreen.Should().Be("settings");
        session.Navigation.NavigationStack.Should().ContainSingle().Which.Should().Be("main");
    }

    [Fact]
    public void PushScreen_ExceedsMaxDepth_DropsOldest()
    {
        var session = new UserSession(1);

        for (int i = 0; i <= UserSession.MAX_NAVIGATION_DEPTH + 5; i++)
            session.Navigation.PushScreen($"screen_{i}");

        session.Navigation.NavigationStack.Should().HaveCount(UserSession.MAX_NAVIGATION_DEPTH);
        session.Navigation.NavigationStack[0].Should().NotBe("screen_0");
    }

    [Fact]
    public void PushScreen_DuplicateOfRoot_ClearsStack()
    {
        var session = new UserSession(1);
        session.Navigation.PushScreen("main");
        session.Navigation.PushScreen("a");
        session.Navigation.PushScreen("b");
        session.Navigation.PushScreen("c");

        session.Navigation.PushScreen("main");

        session.Navigation.CurrentScreen.Should().Be("main");
        session.Navigation.NavigationStack.Should().BeEmpty();
    }
}