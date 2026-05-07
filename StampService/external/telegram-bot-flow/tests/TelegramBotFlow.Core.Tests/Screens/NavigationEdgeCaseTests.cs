using FluentAssertions;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Screens;

/// <summary>
/// Edge cases for NavigationState internal mutation methods (PushScreen, PopScreen, Reset).
/// </summary>
public sealed class NavigationEdgeCaseTests
{
    [Fact]
    public void PopScreen_OnEmptyStack_ReturnsNull()
    {
        var session = new UserSession(1);
        // CurrentScreen is null, stack is empty

        string? result = session.Navigation.PopScreen();

        result.Should().BeNull();
    }

    [Fact]
    public void PushScreen_SameScreenId_IsNoOp()
    {
        var session = new UserSession(1);
        session.Navigation.PushScreen("main");
        session.Navigation.PushScreen("settings");

        // Push the same screen again
        session.Navigation.PushScreen("settings");

        session.Navigation.CurrentScreen.Should().Be("settings");
        session.Navigation.NavigationStack.Should().ContainSingle()
            .Which.Should().Be("main");
    }

    [Fact]
    public void PushScreen_BeyondMaxDepth_DropsOldestEntry()
    {
        var session = new UserSession(1);

        // Push MAX_NAVIGATION_DEPTH + 6 unique screens so the stack fills up
        for (int i = 0; i <= UserSession.MAX_NAVIGATION_DEPTH + 5; i++)
            session.Navigation.PushScreen($"screen_{i}");

        session.Navigation.NavigationStack.Should().HaveCount(UserSession.MAX_NAVIGATION_DEPTH);
        // The very first screen should have been evicted
        session.Navigation.NavigationStack[0].Should().NotBe("screen_0");
    }

    [Fact]
    public void PushScreen_ClearsPendingInputActionId()
    {
        var session = new UserSession(1);
        session.Navigation.PushScreen("start");
        session.Navigation.SetPending("some-action");

        session.Navigation.PushScreen("next");

        session.Navigation.PendingInputActionId.Should().BeNull();
    }

    [Fact]
    public void PopScreen_ClearsPendingInputActionId()
    {
        var session = new UserSession(1);
        session.Navigation.PushScreen("main");
        session.Navigation.PushScreen("settings");
        session.Navigation.SetPending("input-action");

        session.Navigation.PopScreen();

        session.Navigation.PendingInputActionId.Should().BeNull();
    }

    [Fact]
    public void Reset_ClearsStackAndCurrentScreen_ButPreservesNavMessageId()
    {
        var session = new UserSession(1);
        session.Navigation.PushScreen("main");
        session.Navigation.PushScreen("settings");
        // Simulate a NavMessageId being set (internal setter accessible via InternalsVisibleTo)
        session.Navigation.NavMessageId = 42;

        session.Navigation.Reset();

        session.Navigation.CurrentScreen.Should().BeNull();
        session.Navigation.NavigationStack.Should().BeEmpty();
        session.Navigation.PendingInputActionId.Should().BeNull();
        // NavMessageId must be preserved — the bot edits the existing message after reset
        session.Navigation.NavMessageId.Should().Be(42);
    }
}
