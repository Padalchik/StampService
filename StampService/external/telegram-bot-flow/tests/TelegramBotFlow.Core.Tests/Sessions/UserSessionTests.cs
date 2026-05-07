using FluentAssertions;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Sessions;

namespace TelegramBotFlow.Core.Tests.Sessions;

public sealed class UserSessionTests
{
    [Fact]
    public void Set_And_GetString_WorkCorrectly()
    {
        var session = new UserSession(1);
        session.Data.Set("name", "Test");

        session.Data.GetString("name").Should().Be("Test");
    }

    [Fact]
    public void GetString_NonExistentKey_ReturnsNull()
    {
        var session = new UserSession(1);

        session.Data.GetString("missing").Should().BeNull();
    }

    [Fact]
    public void GetInt_ParsesStringToInt()
    {
        var session = new UserSession(1);
        session.Data.Set("age", "25");

        session.Data.GetInt("age").Should().Be(25);
        session.Data.GetInt("missing").Should().BeNull();
    }

    [Fact]
    public void GetBool_ParsesTrueString()
    {
        var session = new UserSession(1);
        session.Data.Set("active", "true");
        session.Data.Set("inactive", "false");

        session.Data.GetBool("active").Should().BeTrue();
        session.Data.GetBool("inactive").Should().BeFalse();
        session.Data.GetBool("missing").Should().BeNull();
    }

    [Fact]
    public void Has_ReturnsTrueForExistingKey()
    {
        var session = new UserSession(1);
        session.Data.Set("key", "value");

        session.Data.Has("key").Should().BeTrue();
        session.Data.Has("other").Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllDataAndResetsNavigation()
    {
        var session = new UserSession(1);
        session.Data.Set("key", "value");
        session.Navigation.CurrentScreen = "settings:main";
        session.Navigation.NavMessageId = 100;
        session.Navigation.PopulateNavigationStack(["main"]);

        session.Clear();

        session.Data.Has("key").Should().BeFalse();
        session.Navigation.CurrentScreen.Should().BeNull();
        session.Navigation.NavMessageId.Should().BeNull();
        session.Navigation.NavigationStack.Should().BeEmpty();
        session.Navigation.CurrentMediaType.Should().Be(ScreenMediaType.None);
    }

    [Fact]
    public void CurrentScreen_CanBeSetAndRead()
    {
        var session = new UserSession(1);

        session.Navigation.CurrentScreen.Should().BeNull();

        session.Navigation.CurrentScreen = "settings:main";

        session.Navigation.CurrentScreen.Should().Be("settings:main");
    }

    [Fact]
    public void PushScreen_AddsCurrentScreenToStack()
    {
        var session = new UserSession(1);

        session.Navigation.PushScreen("main");
        session.Navigation.CurrentScreen.Should().Be("main");
        session.Navigation.NavigationStack.Should().BeEmpty();

        session.Navigation.PushScreen("settings");
        session.Navigation.CurrentScreen.Should().Be("settings");
        session.Navigation.NavigationStack.Should().ContainSingle().Which.Should().Be("main");

        session.Navigation.PushScreen("lang");
        session.Navigation.CurrentScreen.Should().Be("lang");
        session.Navigation.NavigationStack.Should().HaveCount(2);
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

        popped = session.Navigation.PopScreen();
        popped.Should().Be("main");
        session.Navigation.CurrentScreen.Should().Be("main");

        popped = session.Navigation.PopScreen();
        popped.Should().BeNull();
    }

    // -- SessionData.Get<T> / Set<T> --

    [Fact]
    public void Set_Generic_And_Get_Generic_RoundtripObject()
    {
        var session = new UserSession(1);
        var filter = new TestFilter { Category = "tech", Page = 3 };

        session.Data.Set("filters", filter);
        var restored = session.Data.Get<TestFilter>("filters");

        restored.Should().NotBeNull();
        restored!.Category.Should().Be("tech");
        restored.Page.Should().Be(3);
    }

    [Fact]
    public void Get_Generic_MissingKey_ReturnsDefault()
    {
        var session = new UserSession(1);

        int? result = session.Data.Get<int?>("missing");

        result.Should().BeNull();
    }

    [Fact]
    public void Set_Generic_OverwritesPreviousValue()
    {
        var session = new UserSession(1);
        session.Data.Set("counter", 1);
        session.Data.Set("counter", 42);

        session.Data.Get<int>("counter").Should().Be(42);
    }

    [Fact]
    public void Set_Generic_And_GetString_ReturnJsonRepresentation()
    {
        var session = new UserSession(1);
        session.Data.Set("count", 5);

        // GetString должен вернуть JSON-строку "5"
        session.Data.GetString("count").Should().Be("5");
    }

    [Fact]
    public void Set_String_And_Get_Generic_PrimitivesWork()
    {
        var session = new UserSession(1);
        session.Data.Set("page", "7");

        // Get<int> парсит из строки JSON "7"
        session.Data.Get<int>("page").Should().Be(7);
    }

    // Helper type for generic session tests
    private sealed class TestFilter
    {
        public string Category { get; set; } = string.Empty;
        public int Page { get; set; }
    }
}