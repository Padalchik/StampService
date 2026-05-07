using FluentAssertions;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Tests.Wizards;

public sealed class WizardGoBackTests
{
    // ── Test fixtures ──────────────────────────────────────────────────────────

    private sealed class TwoStepState
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class TwoStepWizard : BotWizard<TwoStepState>
    {
        protected override void ConfigureSteps(WizardBuilder<TwoStepState> builder)
        {
            builder
                .TextStep("step1", "Введите значение 1:", (state, text) =>
                {
                    state.Value = text;
                    return StepResult.GoTo("step2");
                })
                .TextStep("step2", state => $"Вы ввели: {state.Value}. Введите значение 2:", (state, _) =>
                    StepResult.Finish());
        }

        public override Task<IEndpointResult> OnFinishedAsync(UpdateContext context, TwoStepState state) =>
            Task.FromResult<IEndpointResult>(BotResults.Empty());
    }

    private sealed class GoBackFromStepWizard : BotWizard<TwoStepState>
    {
        protected override void ConfigureSteps(WizardBuilder<TwoStepState> builder)
        {
            builder
                .Step(
                    id: "step1",
                    renderer: (_, _) => new ScreenView("Step 1"),
                    processor: (_, state) =>
                    {
                        state.Value = "filled";
                        return Task.FromResult<StepResult>(StepResult.GoTo("step2"));
                    })
                .Step(
                    id: "step2",
                    renderer: (_, _) => new ScreenView("Step 2"),
                    processor: (_, _) => Task.FromResult<StepResult>(StepResult.GoBack()));
        }

        public override Task<IEndpointResult> OnFinishedAsync(UpdateContext context, TwoStepState state) =>
            Task.FromResult<IEndpointResult>(BotResults.Empty());
    }

    // ── GoBackAsync tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task GoBackAsync_WithEmptyHistory_ReturnsFinishedTransitionWithBack()
    {
        // Arrange
        var wizard = new TwoStepWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            StepHistory = []
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("test");

        // Act
        WizardTransition transition = await wizard.GoBackAsync(ctx, storageState);

        // Assert
        transition.IsFinished.Should().BeTrue();
        transition.EndpointResult.Should().BeOfType<NavigateBackResult>();
    }

    [Fact]
    public async Task GoBackAsync_WithHistory_ReturnsPreviousStepView()
    {
        // Arrange
        var wizard = new TwoStepWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step2",
            StepHistory = ["step1"]
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("test");

        // Act
        WizardTransition transition = await wizard.GoBackAsync(ctx, storageState);

        // Assert
        transition.IsFinished.Should().BeFalse();
        transition.EndpointResult.Should().BeOfType<ShowViewResult>();
        storageState.CurrentStepId.Should().Be("step1");
        storageState.StepHistory.Should().BeEmpty();
    }

    [Fact]
    public async Task GoBackAsync_PopsOnlyLastItemFromHistory()
    {
        // Arrange — два предыдущих шага (один стек глубиной > 1)
        var wizard = new TwoStepWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step2",
            StepHistory = ["step1", "step1"]
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("test");

        // Act
        await wizard.GoBackAsync(ctx, storageState);

        // Assert — только последний элемент удалён
        storageState.StepHistory.Should().HaveCount(1);
        storageState.CurrentStepId.Should().Be("step1");
    }

    [Fact]
    public async Task GoBackAsync_WithHistory_ReturnsNotFinished()
    {
        var wizard = new TwoStepWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step2",
            StepHistory = ["step1"]
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("test");

        WizardTransition transition = await wizard.GoBackAsync(ctx, storageState);

        transition.IsFinished.Should().BeFalse();
        transition.ShouldDeleteUserMessage.Should().BeFalse();
    }

    // ── ProcessUpdateAsync — history tracking ──────────────────────────────────

    [Fact]
    public async Task ProcessUpdateAsync_GoToResult_PushesCurrentStepToHistory()
    {
        // Arrange
        var wizard = new TwoStepWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            StepHistory = [],
            PayloadJson = "{\"Value\":\"\"}"
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("hello");

        // Act
        await wizard.ProcessUpdateAsync(ctx, storageState);

        // Assert
        storageState.CurrentStepId.Should().Be("step2");
        storageState.StepHistory.Should().ContainSingle().Which.Should().Be("step1");
    }

    [Fact]
    public async Task ProcessUpdateAsync_StayResult_DoesNotPushToHistory()
    {
        // Arrange — empty input triggers Stay
        var wizard = new TwoStepWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            StepHistory = [],
            PayloadJson = "{\"Value\":\"\"}"
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("   "); // whitespace → empty

        // Act
        await wizard.ProcessUpdateAsync(ctx, storageState);

        // Assert — Stay не меняет историю
        storageState.StepHistory.Should().BeEmpty();
        storageState.CurrentStepId.Should().Be("step1");
    }

    [Fact]
    public async Task ProcessUpdateAsync_GoBackResult_GoesBackToPreviousStep()
    {
        // Arrange — визард возвращает GoBack из step2
        var wizard = new GoBackFromStepWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step2",
            StepHistory = ["step1"],
            PayloadJson = "{\"Value\":\"filled\"}"
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("trigger");

        // Act
        WizardTransition transition = await wizard.ProcessUpdateAsync(ctx, storageState);

        // Assert
        transition.IsFinished.Should().BeFalse();
        storageState.CurrentStepId.Should().Be("step1");
        storageState.StepHistory.Should().BeEmpty();
    }

    [Fact]
    public async Task ProcessUpdateAsync_GoBackResult_OnFirstStep_FinishesWizard()
    {
        // Arrange
        var wizard = new GoBackFromStepWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step2",
            StepHistory = [],         // пустая история → выход из визарда
            PayloadJson = "{\"Value\":\"filled\"}"
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("trigger");

        // Act
        WizardTransition transition = await wizard.ProcessUpdateAsync(ctx, storageState);

        // Assert
        transition.IsFinished.Should().BeTrue();
        transition.EndpointResult.Should().BeOfType<NavigateBackResult>();
    }

    // ── StepResult.GoBack factory ──────────────────────────────────────────────

    [Fact]
    public void GoBack_StaticFactory_ReturnsGoBackResult()
    {
        StepResult result = StepResult.GoBack();

        result.Should().BeOfType<StepResult.GoBackResult>();
    }

    [Fact]
    public void GoBackResult_IsSubtypeOfStepResult()
    {
        var result = new StepResult.GoBackResult();

        result.Should().BeAssignableTo<StepResult>();
    }
}