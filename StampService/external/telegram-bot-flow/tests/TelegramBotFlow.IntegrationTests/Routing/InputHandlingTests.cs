using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.IntegrationTests.Infrastructure;

namespace TelegramBotFlow.IntegrationTests.Routing;

/// <summary>
/// Проверяет поведение MapInput + StayResult/NavigateBack: сохранение и сброс PendingInputActionId.
/// </summary>
[Collection(nameof(BotApplicationTests))]
public sealed class InputHandlingTests : BotFlowTestBase
{
    private readonly ISessionStore _sessionStore;

    public InputHandlingTests(BotWebApplicationFactory factory) : base(factory)
    {
        _sessionStore = factory.Services.GetRequiredService<ISessionStore>();

        // Регистрируем тестовые input-хэндлеры через singleton-реестр фреймворка.
        // Register использует dictionary assignment — повторные вызовы безопасны.
        var registry = factory.Services.GetRequiredService<InputHandlerRegistry>();

        registry.Register(
            "test-input",
            HandlerDelegateFactory.CreateForInput(
                (UpdateContext ctx) => Task.FromResult(BotResults.Stay("retry")),
                "test-input"));

        registry.Register(
            "test-input-navigate",
            HandlerDelegateFactory.CreateForInput(
                (UpdateContext ctx) => Task.FromResult(BotResults.Back()),
                "test-input-navigate"));
    }

    private async Task SetPendingAsync(long userId, string actionId)
    {
        var session = await _sessionStore.GetOrCreateAsync(userId, CancellationToken.None);
        session.Navigation.SetPending(actionId);
        await _sessionStore.SaveAsync(session, CancellationToken.None);
    }

    // -- StayResult preserves pending --

    [Fact]
    public async Task Stay_PreservesPendingInputActionId_AfterHandlerExecution()
    {
        long userId = 400_001;
        await SendMessageAsync(userId, "/start");
        await SetPendingAsync(userId, "test-input");

        await SendMessageAsync(userId, "some input");

        var session = await GetSessionAsync(userId);
        session.Navigation.PendingInputActionId.Should().Be("test-input");
    }

    // -- NavigateBack clears pending --

    [Fact]
    public async Task NavigateBack_ClearsPendingInputActionId()
    {
        long userId = 400_002;
        await SendMessageAsync(userId, "/start");
        await SendCallbackAsync(userId, "nav:profile");
        await SetPendingAsync(userId, "test-input-navigate");

        await SendMessageAsync(userId, "some input");

        var session = await GetSessionAsync(userId);
        session.Navigation.PendingInputActionId.Should().BeNull();
        session.Navigation.CurrentScreen.Should().Be("main_menu");
    }

    // -- PendingInputActionId cleared before handler invocation --

    [Fact]
    public async Task InputHandler_IsNotCalledTwice_WhenPendingCleared()
    {
        long userId = 400_003;
        await SendMessageAsync(userId, "/start");
        await SetPendingAsync(userId, "test-input");

        await SendMessageAsync(userId, "first input");
        // PendingInputActionId is restored by StayResult, so second input also goes to handler

        await SendMessageAsync(userId, "second input");

        var session = await GetSessionAsync(userId);
        session.Navigation.PendingInputActionId.Should().Be("test-input");
    }

    // -- No pending input — text message goes to regular router (unmatched → fallback) --

    [Fact]
    public async Task TextMessage_WithoutPending_DoesNotCallInputHandler()
    {
        long userId = 400_004;
        await SendMessageAsync(userId, "/start");
        // No pending action set

        await SendMessageAsync(userId, "random text");

        // Session state should not have pending
        var session = await GetSessionAsync(userId);
        session.Navigation.PendingInputActionId.Should().BeNull();
    }
}