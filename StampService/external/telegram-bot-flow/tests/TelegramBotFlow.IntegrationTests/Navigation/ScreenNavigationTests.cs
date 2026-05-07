using FluentAssertions;
using TelegramBotFlow.IntegrationTests.Infrastructure;

namespace TelegramBotFlow.IntegrationTests.Navigation;

/// <summary>
/// Проверяет полный цикл навигации через реальный pipeline с FakeScreenMessageRenderer.
/// Сценарии: команда → экран, переходы между экранами, back, menu, стек навигации.
/// Тесты проверяют поведение (состояние сессии), а не детали реализации (какие API вызваны).
/// </summary>
[Collection(nameof(BotApplicationTests))]
public sealed class ScreenNavigationTests : BotFlowTestBase
{
    public ScreenNavigationTests(BotWebApplicationFactory factory) : base(factory)
    {
    }

    // -- /start command --

    [Fact]
    public async Task Start_Command_SetsMainMenuAsCurrentScreen()
    {
        long userId = 200_001;

        await SendMessageAsync(userId, "/start");

        var session = await GetSessionAsync(userId);
        session.Navigation.CurrentScreen.Should().Be("main_menu");
    }

    [Fact]
    public async Task Start_Command_LeavesNavigationStackEmpty()
    {
        long userId = 200_002;

        await SendMessageAsync(userId, "/start");

        var session = await GetSessionAsync(userId);
        session.Navigation.NavigationStack.Should().BeEmpty();
    }

    [Fact]
    public async Task Start_Command_StoresNavMessageId()
    {
        long userId = 200_003;

        await SendMessageAsync(userId, "/start");

        var session = await GetSessionAsync(userId);
        session.Navigation.NavMessageId.Should().Be(
            FakeScreenMessageRenderer.DEFAULT_NAV_MESSAGE_ID,
            "FakeScreenMessageRenderer всегда возвращает Message{Id=42} для новых сообщений");
    }

    // -- nav:* callback navigation --

    [Fact]
    public async Task NavCallback_NavigatesTo_SetsCurrentScreen()
    {
        long userId = 200_004;
        await SendMessageAsync(userId, "/start");

        await SendCallbackAsync(userId, "nav:profile");

        var session = await GetSessionAsync(userId);
        session.Navigation.CurrentScreen.Should().Be("profile");
    }

    [Fact]
    public async Task NavCallback_NavigatesTo_PushesCurrentScreenToStack()
    {
        long userId = 200_005;
        await SendMessageAsync(userId, "/start");

        await SendCallbackAsync(userId, "nav:profile");

        var session = await GetSessionAsync(userId);
        session.Navigation.NavigationStack.Should().Equal("main_menu");
    }

    [Fact]
    public async Task NavCallback_Back_ReturnsToMainMenu_WhenOnProfileScreen()
    {
        long userId = 200_006;
        await SendMessageAsync(userId, "/start");
        await SendCallbackAsync(userId, "nav:profile");

        await SendCallbackAsync(userId, "nav:back");

        var session = await GetSessionAsync(userId);
        session.Navigation.CurrentScreen.Should().Be("main_menu");
        session.Navigation.NavigationStack.Should().BeEmpty();
    }

    [Fact]
    public async Task NavCallback_Menu_ClearsStackAndSetsMainMenu_FromDeepNavigation()
    {
        long userId = 200_007;
        await SendMessageAsync(userId, "/start");
        await SendCallbackAsync(userId, "nav:profile");
        await SendCallbackAsync(userId, "nav:settings");

        await SendCallbackAsync(userId, "nav:menu");

        var session = await GetSessionAsync(userId);
        session.Navigation.CurrentScreen.Should().Be("main_menu");
        session.Navigation.NavigationStack.Should().BeEmpty();
    }

    [Fact]
    public async Task NavCallback_Close_RefreshesCurrentScreen_WithoutChangingStack()
    {
        long userId = 200_008;
        await SendMessageAsync(userId, "/start");
        await SendCallbackAsync(userId, "nav:profile");

        await SendCallbackAsync(userId, "nav:close");

        var session = await GetSessionAsync(userId);
        session.Navigation.CurrentScreen.Should().Be("profile");
        session.Navigation.NavigationStack.Should().Equal("main_menu");
    }

    // -- deep navigation and stack integrity --

    [Fact]
    public async Task ThreeLevelNavigation_StackContainsCorrectHistory()
    {
        long userId = 200_009;
        await SendMessageAsync(userId, "/start");
        await SendCallbackAsync(userId, "nav:profile");
        await SendCallbackAsync(userId, "nav:settings");

        var session = await GetSessionAsync(userId);
        session.Navigation.CurrentScreen.Should().Be("settings");
        session.Navigation.NavigationStack.Should().ContainInOrder("main_menu", "profile");
    }

    [Fact]
    public async Task Back_Twice_CorrectlyUnwindsStack()
    {
        long userId = 200_010;
        await SendMessageAsync(userId, "/start");
        await SendCallbackAsync(userId, "nav:profile");
        await SendCallbackAsync(userId, "nav:settings");

        await SendCallbackAsync(userId, "nav:back");
        await SendCallbackAsync(userId, "nav:back");

        var session = await GetSessionAsync(userId);
        session.Navigation.CurrentScreen.Should().Be("main_menu");
        session.Navigation.NavigationStack.Should().BeEmpty();
    }

    // -- AnswerCallbackAsync called on navigation --

    [Fact]
    public async Task NavCallback_AnswersCallbackExactlyOnce()
    {
        long userId = 200_011;
        await SendMessageAsync(userId, "/start");
        ClearMockCalls();

        await SendCallbackAsync(userId, "nav:profile");

        VerifyCallbackAnswered(times: 1);
    }

    [Fact]
    public async Task NavBack_AnswersCallbackExactlyOnce()
    {
        long userId = 200_012;
        await SendMessageAsync(userId, "/start");
        await SendCallbackAsync(userId, "nav:profile");
        ClearMockCalls();

        await SendCallbackAsync(userId, "nav:back");

        VerifyCallbackAnswered(times: 1);
    }
}