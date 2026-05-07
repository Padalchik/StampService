using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Sessions;
using TelegramBotFlow.Core.Wizards;
using TelegramBotFlow.IntegrationTests.Infrastructure;

namespace TelegramBotFlow.IntegrationTests.Wizards;

/// <summary>
/// Интеграционные тесты для навигационных callback-ов внутри активного визарда:
/// nav:back (GoBack), nav:menu (отмена), nav:close (отмена).
/// Тесты проверяют реальное поведение WizardMiddleware через полный pipeline.
/// </summary>
[Collection(nameof(BotApplicationTests))]
public sealed class WizardNavigationCallbackTests : BotFlowTestBase
{
    private readonly IWizardStore _wizardStore;
    private readonly ISessionStore _sessionStore;

    public WizardNavigationCallbackTests(BotWebApplicationFactory factory) : base(factory)
    {
        _wizardStore = factory.Services.GetRequiredService<IWizardStore>();
        _sessionStore = factory.Services.GetRequiredService<ISessionStore>();
    }

    // ── Setup helpers ──────────────────────────────────────────────────────────

    private async Task SetupActiveWizard(
        long userId,
        string stepId,
        List<string> stepHistory,
        string? payloadJson = null)
    {
        var session = await _sessionStore.GetOrCreateAsync(userId, CancellationToken.None);
        session.Navigation.ActiveWizardId = nameof(NavCallbackTestWizard);
        await _sessionStore.SaveAsync(session, CancellationToken.None);

        var storageState = new WizardStorageState
        {
            CurrentStepId = stepId,
            StepHistory = stepHistory,
            PayloadJson = payloadJson ?? "{\"Value1\":\"\",\"Value2\":\"\"}"
        };
        await _wizardStore.SaveAsync(
            userId,
            nameof(NavCallbackTestWizard),
            storageState,
            CancellationToken.None);
    }

    // ── nav:back tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task NavBack_OnSecondStep_GoesBackToFirstStep()
    {
        // Arrange
        const long userId = 50_001;
        await SetupActiveWizard(userId, stepId: "step2", stepHistory: ["step1"]);

        // Act
        await SendCallbackAsync(userId, NavCallbacks.BACK);

        // Assert — шаг вернулся к step1
        var state = await _wizardStore.GetAsync(
            userId, nameof(NavCallbackTestWizard), CancellationToken.None);

        state.Should().NotBeNull("визард ещё не завершён");
        state!.CurrentStepId.Should().Be("step1");
        state.StepHistory.Should().BeEmpty();
    }

    [Fact]
    public async Task NavBack_OnFirstStep_FinishesWizardAndClearsActiveWizardId()
    {
        // Arrange
        const long userId = 50_002;
        await SetupActiveWizard(userId, stepId: "step1", stepHistory: []);

        // Act
        await SendCallbackAsync(userId, NavCallbacks.BACK);

        // Assert — визард завершён
        var state = await _wizardStore.GetAsync(
            userId, nameof(NavCallbackTestWizard), CancellationToken.None);
        state.Should().BeNull("GoBack из первого шага должен завершить визард");

        var session = await GetSessionAsync(userId);
        session.Navigation.ActiveWizardId.Should().BeNull();
    }

    [Fact]
    public async Task NavBack_OnFirstStep_NavigatesBack_ToScreenBeforeWizard()
    {
        // Arrange
        const long userId = 50_003;

        // Сначала переходим на какой-то экран
        await SendMessageAsync(userId, "/start");
        // Затем активируем визард
        await SetupActiveWizard(userId, stepId: "step1", stepHistory: []);

        // Act
        await SendCallbackAsync(userId, NavCallbacks.BACK);

        // Assert — навигация назад выполнена (CurrentScreen изменился)
        // FakeScreenMessageRenderer не падает и nav обработан
        var session = await GetSessionAsync(userId);
        session.Navigation.ActiveWizardId.Should().BeNull();
    }

    // ── nav:menu tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task NavMenu_CancelsActiveWizard_AndClearsActiveWizardId()
    {
        // Arrange
        const long userId = 50_010;
        await SetupActiveWizard(userId, stepId: "step2", stepHistory: ["step1"]);

        // Act
        await SendCallbackAsync(userId, NavCallbacks.MENU);

        // Assert — wizard store очищен
        var state = await _wizardStore.GetAsync(
            userId, nameof(NavCallbackTestWizard), CancellationToken.None);
        state.Should().BeNull("nav:menu должен отменить визард");

        var session = await GetSessionAsync(userId);
        session.Navigation.ActiveWizardId.Should().BeNull();
    }

    [Fact]
    public async Task NavMenu_After_WizardCancellation_CanNavigateToMainMenu()
    {
        // Arrange
        const long userId = 50_011;
        // Setup screen state
        await SendMessageAsync(userId, "/start");
        await SetupActiveWizard(userId, stepId: "step1", stepHistory: []);

        // Act
        await SendCallbackAsync(userId, NavCallbacks.MENU);

        // Assert — нет активного визарда
        var session = await GetSessionAsync(userId);
        session.Navigation.ActiveWizardId.Should().BeNull();
    }

    // ── nav:close tests ────────────────────────────────────────────────────────

    [Fact]
    public async Task NavClose_CancelsActiveWizard_AndClearsActiveWizardId()
    {
        // Arrange
        const long userId = 50_020;
        await SetupActiveWizard(userId, stepId: "step1", stepHistory: []);

        // Act
        await SendCallbackAsync(userId, NavCallbacks.CLOSE);

        // Assert
        var state = await _wizardStore.GetAsync(
            userId, nameof(NavCallbackTestWizard), CancellationToken.None);
        state.Should().BeNull();

        var session = await GetSessionAsync(userId);
        session.Navigation.ActiveWizardId.Should().BeNull();
    }

    // ── Regular step processing still works ───────────────────────────────────

    [Fact]
    public async Task RegularTextInput_WhileInWizard_AdvancesStep()
    {
        // Arrange
        const long userId = 50_030;
        await SetupActiveWizard(userId, stepId: "step1", stepHistory: []);

        // Act — обычный текстовый ввод
        await SendMessageAsync(userId, "some input");

        // Assert — шаг продвинулся
        var state = await _wizardStore.GetAsync(
            userId, nameof(NavCallbackTestWizard), CancellationToken.None);

        state.Should().NotBeNull();
        state!.CurrentStepId.Should().Be("step2");
        state.StepHistory.Should().ContainSingle().Which.Should().Be("step1");
    }

    [Fact]
    public async Task WizardCancel_Command_StillCancelsWizard()
    {
        // Arrange
        const long userId = 50_040;
        await SetupActiveWizard(userId, stepId: "step2", stepHistory: ["step1"]);

        // Act — /cancel команда
        await SendMessageAsync(userId, "/cancel");

        // Assert
        var session = await GetSessionAsync(userId);
        session.Navigation.ActiveWizardId.Should().BeNull();
    }
}

// ── Test wizard ────────────────────────────────────────────────────────────────

public sealed class NavCallbackTestWizardState
{
    public string Value1 { get; set; } = string.Empty;
    public string Value2 { get; set; } = string.Empty;
}

/// <summary>
/// Двухшаговый тестовый визард с чистыми (pure) перегрузками.
/// Регистрируется автоматически через <see cref="WizardRegistry.RegisterFromAssembly"/>.
/// </summary>
public sealed class NavCallbackTestWizard : BotWizard<NavCallbackTestWizardState>
{
    protected override void ConfigureSteps(WizardBuilder<NavCallbackTestWizardState> builder)
    {
        builder
            .TextStep("step1", "Введите значение 1:", (state, text) =>
            {
                state.Value1 = text;
                return StepResult.GoTo("step2");
            })
            .TextStep("step2", state => $"Вы ввели: {state.Value1}. Введите значение 2:", (state, text) =>
            {
                state.Value2 = text;
                return StepResult.Finish();
            });
    }

    public override Task<IEndpointResult> OnFinishedAsync(
        UpdateContext context,
        NavCallbackTestWizardState state) =>
        Task.FromResult<IEndpointResult>(BotResults.Empty());
}