using System.Text.Json;
using FluentAssertions;
using TelegramBotFlow.Core.Routing;
using TelegramBotFlow.Core.Context;
using TelegramBotFlow.Core.Screens;
using TelegramBotFlow.Core.Wizards;

namespace TelegramBotFlow.Core.Tests.Wizards;

public sealed class WizardCancellationTests
{
    // ── Test fixtures ──────────────────────────────────────────────────────────

    private sealed class TestState
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class DefaultCancelWizard : BotWizard<TestState>
    {
        protected override void ConfigureSteps(WizardBuilder<TestState> builder)
        {
            builder.Step(
                id: "step1",
                renderer: (_, _) => new ScreenView("Enter name:"),
                processor: (_, state) =>
                {
                    state.Name = "filled";
                    return Task.FromResult<StepResult>(StepResult.GoTo("step2"));
                })
            .Step(
                id: "step2",
                renderer: (_, _) => new ScreenView("Confirm?"),
                processor: (_, _) => Task.FromResult<StepResult>(StepResult.Finish()));
        }

        public override Task<IEndpointResult> OnFinishedAsync(UpdateContext context, TestState state) =>
            Task.FromResult<IEndpointResult>(BotResults.Empty());
    }

    private sealed class TrackingCancelWizard : BotWizard<TestState>
    {
        public bool WasCancelledCalled { get; private set; }
        public TestState? CancelledState { get; private set; }

        protected override void ConfigureSteps(WizardBuilder<TestState> builder)
        {
            builder.Step(
                id: "step1",
                renderer: (_, _) => new ScreenView("Enter name:"),
                processor: (_, _) => Task.FromResult<StepResult>(StepResult.GoBack()));
        }

        public override Task<IEndpointResult> OnFinishedAsync(UpdateContext context, TestState state) =>
            Task.FromResult<IEndpointResult>(BotResults.Empty());

        protected override Task OnCancelledAsync(UpdateContext context, TestState state)
        {
            WasCancelledCalled = true;
            CancelledState = state;
            return Task.CompletedTask;
        }
    }

    // ── Tests ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DefaultOnCancelledAsync_DoesNothing()
    {
        // Arrange — wizard with no override (default no-op)
        IBotWizard wizard = new DefaultCancelWizard();
        UpdateContext ctx = TestHelpers.CreateMessageContext("test");
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            PayloadJson = JsonSerializer.Serialize(new TestState { Name = "hello" })
        };

        // Act — should complete without throwing
        await wizard.OnCancelledAsync(ctx, storageState);
    }

    [Fact]
    public async Task OnCancelledAsync_CalledViaInterface_DeserializesState()
    {
        // Arrange
        var wizard = new TrackingCancelWizard();
        IBotWizard wizardInterface = wizard;
        UpdateContext ctx = TestHelpers.CreateMessageContext("test");
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            PayloadJson = JsonSerializer.Serialize(new TestState { Name = "test-value" })
        };

        // Act
        await wizardInterface.OnCancelledAsync(ctx, storageState);

        // Assert
        wizard.WasCancelledCalled.Should().BeTrue();
        wizard.CancelledState.Should().NotBeNull();
        wizard.CancelledState!.Name.Should().Be("test-value");
    }

    [Fact]
    public async Task OnCancelledAsync_WithMalformedJson_UsesDefaultState()
    {
        // Arrange
        var wizard = new TrackingCancelWizard();
        IBotWizard wizardInterface = wizard;
        UpdateContext ctx = TestHelpers.CreateMessageContext("test");
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            PayloadJson = "not-valid-json{"
        };

        // Act — should not throw
        await wizardInterface.OnCancelledAsync(ctx, storageState);

        // Assert
        wizard.WasCancelledCalled.Should().BeTrue();
        wizard.CancelledState.Should().NotBeNull();
        wizard.CancelledState!.Name.Should().Be(string.Empty); // default TState
    }

    [Fact]
    public async Task OnCancelledAsync_WithEmptyPayload_UsesDefaultState()
    {
        // Arrange
        var wizard = new TrackingCancelWizard();
        IBotWizard wizardInterface = wizard;
        UpdateContext ctx = TestHelpers.CreateMessageContext("test");
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            PayloadJson = ""
        };

        // Act
        await wizardInterface.OnCancelledAsync(ctx, storageState);

        // Assert
        wizard.WasCancelledCalled.Should().BeTrue();
        wizard.CancelledState!.Name.Should().Be(string.Empty);
    }

    [Fact]
    public async Task GoBackAsync_EmptyHistory_SetsWasCancelledFlag()
    {
        // Arrange
        var wizard = new DefaultCancelWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            StepHistory = [],
            PayloadJson = "{}"
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("test");

        // Act
        WizardTransition transition = await wizard.GoBackAsync(ctx, storageState);

        // Assert
        transition.IsFinished.Should().BeTrue();
        transition.WasCancelled.Should().BeTrue();
    }

    [Fact]
    public async Task GoBackAsync_WithHistory_DoesNotSetWasCancelledFlag()
    {
        // Arrange
        var wizard = new DefaultCancelWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step2",
            StepHistory = ["step1"],
            PayloadJson = "{}"
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("test");

        // Act
        WizardTransition transition = await wizard.GoBackAsync(ctx, storageState);

        // Assert
        transition.IsFinished.Should().BeFalse();
        transition.WasCancelled.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessUpdateAsync_FinishResult_DoesNotSetWasCancelledFlag()
    {
        // Arrange — step2 returns Finish
        var wizard = new DefaultCancelWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step2",
            StepHistory = ["step1"],
            PayloadJson = JsonSerializer.Serialize(new TestState { Name = "filled" })
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("confirm");

        // Act
        WizardTransition transition = await wizard.ProcessUpdateAsync(ctx, storageState);

        // Assert
        transition.IsFinished.Should().BeTrue();
        transition.WasCancelled.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessUpdateAsync_GoBackFromFirstStep_SetsWasCancelledFlag()
    {
        // Arrange — TrackingCancelWizard step1 processor returns GoBack, empty history
        var wizard = new TrackingCancelWizard();
        var storageState = new WizardStorageState
        {
            CurrentStepId = "step1",
            StepHistory = [],
            PayloadJson = "{}"
        };
        UpdateContext ctx = TestHelpers.CreateMessageContext("trigger");

        // Act
        WizardTransition transition = await wizard.ProcessUpdateAsync(ctx, storageState);

        // Assert
        transition.IsFinished.Should().BeTrue();
        transition.WasCancelled.Should().BeTrue();
    }
}
