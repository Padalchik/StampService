using FluentAssertions;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Tests.Wizards;

public sealed class BotWizardDeserializationTests
{
    // ── Test fixtures ──────────────────────────────────────────────────────────

    private sealed class SimpleState
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class SimpleWizard : BotWizard<SimpleState>
    {
        protected override void ConfigureSteps(WizardBuilder<SimpleState> builder)
        {
            builder.Step(
                id: "step1",
                renderer: (_, _) => new ScreenView("Step 1"),
                processor: (_, state) =>
                {
                    state.Value = "processed";
                    return Task.FromResult<StepResult>(StepResult.GoTo("step2"));
                })
            .Step(
                id: "step2",
                renderer: (_, _) => new ScreenView("Step 2"),
                processor: (_, _) => Task.FromResult<StepResult>(StepResult.Finish()));
        }

        public override Task<IEndpointResult> OnFinishedAsync(UpdateContext context, SimpleState state) =>
            Task.FromResult<IEndpointResult>(BotResults.Empty());
    }

    private sealed class OnEnterThrowingWizard : BotWizard<SimpleState>
    {
        protected override void ConfigureSteps(WizardBuilder<SimpleState> builder)
        {
            builder
                .Step(
                    id: "step1",
                    renderer: (_, state) => new ScreenView($"Step 1: {state.Value}"),
                    processor: (_, state) =>
                    {
                        state.Value = "from-step1";
                        return Task.FromResult<StepResult>(StepResult.GoTo("step2"));
                    })
                .Step(
                    id: "step2",
                    renderer: (_, _) => new ScreenView("Step 2"),
                    processor: (_, _) => Task.FromResult<StepResult>(StepResult.Finish()),
                    onEnter: (_, state) =>
                    {
                        // Partially mutate state before throwing
                        state.Value = "corrupted";
                        throw new InvalidOperationException("OnEnter failed");
                    });
        }

        public override Task<IEndpointResult> OnFinishedAsync(UpdateContext context, SimpleState state) =>
            Task.FromResult<IEndpointResult>(BotResults.Empty());
    }

    private sealed class InitOnEnterThrowingWizard : BotWizard<SimpleState>
    {
        protected override void ConfigureSteps(WizardBuilder<SimpleState> builder)
        {
            builder.Step(
                id: "step1",
                renderer: (_, _) => new ScreenView("Step 1"),
                processor: (_, _) => Task.FromResult<StepResult>(StepResult.Finish()),
                onEnter: (_, _) => throw new InvalidOperationException("InitOnEnter failed"));
        }

        public override Task<IEndpointResult> OnFinishedAsync(UpdateContext context, SimpleState state) =>
            Task.FromResult<IEndpointResult>(BotResults.Empty());
    }

    // ── Bug 1: ProcessUpdateAsync — malformed JSON ─────────────────────────────

    [Fact]
    public async Task ProcessUpdateAsync_MalformedJson_TerminatesWizardGracefully()
    {
        // Arrange
        var wizard = new SimpleWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            StepHistory = [],
            PayloadJson = "{ this is not valid json !!!"
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("hello");

        // Act
        WizardTransition transition = await wizard.ProcessUpdateAsync(ctx, storageState);

        // Assert
        transition.IsFinished.Should().BeTrue();
        transition.EndpointResult.Should().BeOfType<NavigateBackResult>();
    }

    [Fact]
    public async Task ProcessUpdateAsync_NullOrEmptyPayloadJson_CreatesDefaultState_NoCrash()
    {
        // Arrange — empty payload → default state, no exception
        var wizard = new SimpleWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            StepHistory = [],
            PayloadJson = string.Empty
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("hello");

        // Act
        WizardTransition transition = await wizard.ProcessUpdateAsync(ctx, storageState);

        // Assert — processes normally with default state
        transition.IsFinished.Should().BeFalse();
        storageState.CurrentStepId.Should().Be("step2");
    }

    [Fact]
    public async Task ProcessUpdateAsync_WhitespacePayloadJson_CreatesDefaultState_NoCrash()
    {
        // Arrange — whitespace payload → same as empty
        var wizard = new SimpleWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            StepHistory = [],
            PayloadJson = "   "
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("hello");

        // Act
        WizardTransition transition = await wizard.ProcessUpdateAsync(ctx, storageState);

        // Assert
        transition.IsFinished.Should().BeFalse();
        storageState.CurrentStepId.Should().Be("step2");
    }

    // ── Bug 1: GoBackAsync — malformed JSON ────────────────────────────────────

    [Fact]
    public async Task GoBackAsync_MalformedJson_TerminatesWizardGracefully()
    {
        // Arrange — non-empty history so GoBack tries to deserialize
        var wizard = new SimpleWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step2",
            StepHistory = ["step1"],
            PayloadJson = "<<<not json at all>>>"
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("back");

        // Act
        WizardTransition transition = await wizard.GoBackAsync(ctx, storageState);

        // Assert
        transition.IsFinished.Should().BeTrue();
        transition.EndpointResult.Should().BeOfType<NavigateBackResult>();
    }

    // ── Bug 2: OnEnter partial state corruption ────────────────────────────────

    [Fact]
    public async Task ProcessUpdateAsync_OnEnterThrows_StateIsNotCorrupted()
    {
        // Arrange
        var wizard = new OnEnterThrowingWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            StepHistory = [],
            PayloadJson = "{\"Value\":\"original\"}"
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("hello");

        // Act — processor moves to step2 whose OnEnter throws after mutating state
        WizardTransition transition = await wizard.ProcessUpdateAsync(ctx, storageState);

        // Assert — wizard stays on current step (not finished), state is not corrupted
        transition.IsFinished.Should().BeFalse();
        transition.EndpointResult.Should().BeOfType<ShowViewResult>();
        // The serialized state must NOT contain the corrupted value from OnEnter
        storageState.PayloadJson.Should().NotContain("corrupted");
    }

    [Fact]
    public async Task ProcessUpdateAsync_OnEnterThrows_RendersCurrentStepView()
    {
        // Arrange
        var wizard = new OnEnterThrowingWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            StepHistory = [],
            PayloadJson = "{\"Value\":\"original\"}"
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("hello");

        // Act
        WizardTransition transition = await wizard.ProcessUpdateAsync(ctx, storageState);

        // Assert — renderer for step1 is called with the state as it was just before OnEnter
        // (processor already ran and set Value = "from-step1"; snapshot captures that value).
        // Importantly, "corrupted" (the value set inside the throwing OnEnter) must NOT appear.
        transition.IsFinished.Should().BeFalse();
        var showView = transition.EndpointResult.Should().BeOfType<ShowViewResult>().Subject;
        showView.View.Text.Should().Contain("from-step1");
        showView.View.Text.Should().NotContain("corrupted");
    }

    // ── Bug 2: InitializeAsync — OnEnter throws ────────────────────────────────

    [Fact]
    public async Task InitializeAsync_OnEnterThrows_TerminatesWizardGracefully()
    {
        // Arrange
        var wizard = new InitOnEnterThrowingWizard();
        var storageState = new WizardStorageState();
        UpdateContext ctx = TestHelpers.CreateMessageContext("start");

        // Act
        WizardTransition transition = await wizard.InitializeAsync(ctx, storageState);

        // Assert
        transition.IsFinished.Should().BeTrue();
        transition.EndpointResult.Should().BeOfType<NavigateBackResult>();
    }
}
